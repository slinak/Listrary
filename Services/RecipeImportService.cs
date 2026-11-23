using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ListManager.Services;

/// <summary>
/// Reads schema.org Recipe JSON-LD (used by nearly all recipe sites) from a page.
/// </summary>
public partial class RecipeImportService : IRecipeImportService
{
    public const string HttpClientName = "RecipeImport";
    private const int MaxBytes = 2 * 1024 * 1024;
    private const int MaxIngredients = 200;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RecipeImportService> _logger;

    public RecipeImportService(IHttpClientFactory httpClientFactory, ILogger<RecipeImportService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<RecipeImportResult> ImportFromUrlAsync(string? url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return RecipeImportResult.Failure("Enter a valid http or https recipe URL.");

        string html;
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            html = await ReadLimitedAsync(response, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidDataException or IOException)
        {
            _logger.LogWarning(ex, "Recipe import failed for {Url}", uri);
            return RecipeImportResult.Failure("Couldn't load that page. Check the URL and try again.");
        }

        var result = ParseRecipe(html);
        return result ?? RecipeImportResult.Failure("No recipe ingredients were found on that page.");
    }

    public static RecipeImportResult? ParseRecipe(string html)
    {
        foreach (Match match in JsonLdRegex().Matches(html))
        {
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(WebUtility.HtmlDecode(match.Groups[1].Value).Trim());
            }
            catch (JsonException)
            {
                continue;
            }

            using (document)
            {
                var recipe = FindRecipe(document.RootElement);
                if (recipe is not { } element || !element.TryGetProperty("recipeIngredient", out var ingredientsElement))
                    continue;

                var ingredients = ingredientsElement.ValueKind == JsonValueKind.Array
                    ? ingredientsElement.EnumerateArray()
                    : Enumerable.Empty<JsonElement>();

                var list = ingredients
                    .Where(i => i.ValueKind == JsonValueKind.String)
                    .Select(i => WebUtility.HtmlDecode(i.GetString() ?? string.Empty).Trim())
                    .Where(i => i.Length > 0)
                    .Select(i => i.Length > 500 ? i[..500] : i)
                    .Take(MaxIngredients)
                    .ToList();
                if (list.Count == 0)
                    continue;

                var title = element.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String
                    ? WebUtility.HtmlDecode(name.GetString() ?? string.Empty).Trim()
                    : string.Empty;
                if (title.Length > 200)
                    title = title[..200];

                return new RecipeImportResult(title.Length == 0 ? null : title, list, null);
            }
        }

        return null;
    }

    private static JsonElement? FindRecipe(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    if (FindRecipe(item) is { } found)
                        return found;
                break;
            case JsonValueKind.Object:
                if (element.TryGetProperty("@type", out var type) && IsRecipeType(type))
                    return element;
                if (element.TryGetProperty("@graph", out var graph))
                    return FindRecipe(graph);
                break;
        }

        return null;
    }

    private static bool IsRecipeType(JsonElement type) => type.ValueKind switch
    {
        JsonValueKind.String => string.Equals(type.GetString(), "Recipe", StringComparison.OrdinalIgnoreCase),
        JsonValueKind.Array => type.EnumerateArray().Any(IsRecipeType),
        _ => false
    };

    private static async Task<string> ReadLimitedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > MaxBytes)
                throw new InvalidDataException("Page is too large.");
        }

        return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }

    /// <summary>
    /// Connect callback that refuses loopback/private/link-local targets so a user-supplied
    /// URL (or redirect) can't be used to reach internal services.
    /// </summary>
    public static async ValueTask<Stream> ConnectPublicOnlyAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
        var address = addresses.FirstOrDefault(a => !IsPrivate(a))
            ?? throw new HttpRequestException("Target address is not allowed.");
        if (addresses.Any(IsPrivate))
            throw new HttpRequestException("Target address is not allowed.");

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static bool IsPrivate(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
            return true;
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
            return (address.GetAddressBytes()[0] & 0xFE) == 0xFC || address.Equals(IPAddress.IPv6None) || address.Equals(IPAddress.IPv6Any);

        var b = address.GetAddressBytes();
        return b[0] == 0 || b[0] == 10 || b[0] == 127 || b[0] >= 224
            || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)
            || (b[0] == 169 && b[1] == 254)
            || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
            || (b[0] == 192 && b[1] == 168);
    }

    [GeneratedRegex("<script[^>]*type\\s*=\\s*[\"']application/ld\\+json[\"'][^>]*>(.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex JsonLdRegex();
}
