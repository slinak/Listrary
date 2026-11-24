using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ListManager.Services;

/// <summary>
/// Downloads a recipe page and extracts its title, ingredients and instructions.
/// Looks first at schema.org Recipe JSON-LD (used by nearly all recipe sites), then at
/// schema.org microdata, then at common recipe-plugin markup.
/// </summary>
public partial class RecipeImportService : IRecipeImportService
{
    public const string HttpClientName = "RecipeImport";
    public const int MaxIngredients = 200;
    public const int MaxIngredientLength = 500;
    public const int MaxInstructionsLength = 20000;
    public const int MaxTitleLength = 200;
    private const int MaxBytes = 4 * 1024 * 1024;
    private const int MaxDepth = 10;

    public const string InvalidUrlMessage = "Enter a valid http or https recipe URL.";
    public const string LoadFailedMessage = "Couldn't load that page. Check the URL and try again.";
    public const string BlockedMessage = "That website blocks automatic recipe imports. Open the recipe in your browser, then copy and paste (or screenshot) the ingredients and instructions below.";
    public const string NotFoundMessage = "No recipe was found on that page. Copy and paste (or screenshot) the ingredients and instructions below instead.";

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
            return RecipeImportResult.Failure(InvalidUrlMessage);

        string html;
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (IsBlocked(response))
            {
                _logger.LogInformation("Recipe import blocked by {Host} with status {Status}", uri.Host, (int)response.StatusCode);
                return RecipeImportResult.Failure(BlockedMessage);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Recipe import got status {Status} from {Url}", (int)response.StatusCode, uri);
                return RecipeImportResult.Failure(LoadFailedMessage);
            }

            html = await ReadLimitedAsync(response, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Any network, TLS, DNS, timeout or decoding failure becomes a friendly message.
            _logger.LogWarning(ex, "Recipe import failed for {Url}", uri);
            return RecipeImportResult.Failure(LoadFailedMessage);
        }

        try
        {
            return ParseRecipe(html) ?? RecipeImportResult.Failure(NotFoundMessage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Recipe parsing failed for {Url}", uri);
            return RecipeImportResult.Failure(NotFoundMessage);
        }
    }

    private static bool IsBlocked(HttpResponseMessage response) =>
        response.Headers.Contains("cf-mitigated")
        || response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized or HttpStatusCode.TooManyRequests
        || (response.StatusCode == HttpStatusCode.ServiceUnavailable && response.Headers.Server.ToString().Contains("cloudflare", StringComparison.OrdinalIgnoreCase));

    public static RecipeImportResult? ParseRecipe(string html)
    {
        var parsed = ParseJsonLd(html) ?? ParseMicrodata(html) ?? ParseCommonMarkup(html);
        if (parsed == null)
            return null;

        var (title, ingredients, instructions) = parsed.Value;
        if (string.IsNullOrEmpty(title))
            title = PageTitle(html);

        return new RecipeImportResult(
            Truncate(title, MaxTitleLength),
            ingredients,
            string.IsNullOrEmpty(instructions) ? null : Truncate(instructions, MaxInstructionsLength),
            null);
    }

    // ---------- JSON-LD ----------

    private static (string? Title, List<string> Ingredients, string? Instructions)? ParseJsonLd(string html)
    {
        foreach (Match match in JsonLdRegex().Matches(html))
        {
            JsonDocument document;
            try
            {
                var json = match.Groups[1].Value.Trim();
                if (json.StartsWith("<!--"))
                    json = json[4..];
                if (json.EndsWith("-->"))
                    json = json[..^3];
                document = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            }
            catch (JsonException)
            {
                continue;
            }

            using (document)
            {
                if (FindRecipe(document.RootElement, 0) is not { } recipe)
                    continue;

                var ingredients = new List<string>();
                if (recipe.TryGetProperty("recipeIngredient", out var ingredientsElement)
                    || recipe.TryGetProperty("ingredients", out ingredientsElement))
                    ReadIngredients(ingredientsElement, ingredients, 0);
                ingredients = NormalizeIngredients(ingredients);

                var steps = new List<Step>();
                if (recipe.TryGetProperty("recipeInstructions", out var instructionsElement))
                    ReadInstructions(instructionsElement, steps, 0);
                var instructions = FormatInstructions(steps);

                if (ingredients.Count == 0 && instructions == null)
                    continue;

                return (ReadString(recipe, "name") ?? ReadString(recipe, "headline"), ingredients, instructions);
            }
        }

        return null;
    }

    private static JsonElement? FindRecipe(JsonElement element, int depth)
    {
        if (depth > MaxDepth)
            return null;

        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    if (FindRecipe(item, depth + 1) is { } found)
                        return found;
                break;
            case JsonValueKind.Object:
                if (element.TryGetProperty("@type", out var type) && IsRecipeType(type))
                    return element;
                foreach (var name in new[] { "@graph", "mainEntity", "mainEntityOfPage", "itemListElement", "item" })
                    if (element.TryGetProperty(name, out var child) && FindRecipe(child, depth + 1) is { } nested)
                        return nested;
                break;
        }

        return null;
    }

    private static bool IsRecipeType(JsonElement type) => type.ValueKind switch
    {
        JsonValueKind.String => type.GetString() is { } t
            && (t.Equals("Recipe", StringComparison.OrdinalIgnoreCase) || t.EndsWith("/Recipe", StringComparison.OrdinalIgnoreCase)),
        JsonValueKind.Array => type.EnumerateArray().Any(IsRecipeType),
        _ => false
    };

    private static void ReadIngredients(JsonElement element, List<string> output, int depth)
    {
        if (depth > MaxDepth)
            return;

        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                output.AddRange(SplitLines(CleanHtml(element.GetString())));
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    ReadIngredients(item, output, depth + 1);
                break;
            case JsonValueKind.Object:
                if ((ReadString(element, "text") ?? ReadString(element, "name")) is { } text)
                    output.Add(text);
                break;
        }
    }

    private record Step(string Text, bool IsHeading);

    private static void ReadInstructions(JsonElement element, List<Step> output, int depth)
    {
        if (depth > MaxDepth)
            return;

        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                output.AddRange(SplitLines(CleanHtml(element.GetString())).Select(line => new Step(line, false)));
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    ReadInstructions(item, output, depth + 1);
                break;
            case JsonValueKind.Object:
                var hasChildren = element.TryGetProperty("itemListElement", out var children);
                if (hasChildren)
                {
                    // HowToSection (or an ItemList of steps): optional heading, then its steps.
                    if (ReadString(element, "name") is { } heading)
                        output.Add(new Step(heading, true));
                    ReadInstructions(children, output, depth + 1);
                }
                else if ((ReadString(element, "text") ?? ReadString(element, "name") ?? ReadString(element, "description")) is { } text)
                {
                    output.AddRange(SplitLines(text).Select(line => new Step(line, false)));
                }
                break;
        }
    }

    private static string? ReadString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
            return null;
        if (value.ValueKind == JsonValueKind.Array)
            value = value.EnumerateArray().FirstOrDefault(v => v.ValueKind == JsonValueKind.String);
        if (value.ValueKind != JsonValueKind.String)
            return null;

        var text = CleanHtml(value.GetString());
        return text.Length == 0 ? null : text;
    }

    // ---------- Microdata ----------

    private static (string? Title, List<string> Ingredients, string? Instructions)? ParseMicrodata(string html)
    {
        var ingredients = NormalizeIngredients(ItemPropRegex("recipeIngredient|ingredients").Matches(html)
            .Select(m => CleanHtml(m.Groups["content"].Success ? m.Groups["content"].Value : m.Groups["body"].Value))
            .ToList());

        var steps = ItemPropRegex("recipeInstructions").Matches(html)
            .SelectMany(m => SplitLines(CleanHtml(m.Groups["content"].Success ? m.Groups["content"].Value : m.Groups["body"].Value)))
            .Select(line => new Step(line, false))
            .ToList();
        var instructions = FormatInstructions(steps);

        return ingredients.Count == 0 ? null : (null, ingredients, instructions);
    }

    private static Regex ItemPropRegex(string names) => new(
        "<(?<tag>[a-z0-9]+)\\b[^>]*\\bitemprop\\s*=\\s*[\"'](?:" + names + ")[\"'][^>]*?(?:\\bcontent\\s*=\\s*\"(?<content>[^\"]*)\"[^>]*)?(?:/>|>(?<body>.*?)</\\k<tag>\\s*>)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromSeconds(2));

    // ---------- Common recipe-plugin markup (WP Recipe Maker, Tasty, Dotdash, etc.) ----------

    private static (string? Title, List<string> Ingredients, string? Instructions)? ParseCommonMarkup(string html)
    {
        var ingredients = NormalizeIngredients(ListItemsWithClass(html, "ingredient").ToList());
        if (ingredients.Count == 0)
            return null;

        var steps = ListItemsWithClass(html, "instruction")
            .Concat(ListItemsWithClass(html, "direction"))
            .Select(line => new Step(line, false))
            .ToList();
        return (null, ingredients, FormatInstructions(steps));
    }

    private static IEnumerable<string> ListItemsWithClass(string html, string classFragment)
    {
        var regex = new Regex(
            "<li\\b[^>]*\\bclass\\s*=\\s*[\"'][^\"']*" + Regex.Escape(classFragment) + "[^\"']*[\"'][^>]*>(?<body>.*?)</li\\s*>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromSeconds(2));
        return regex.Matches(html).Select(m => CleanHtml(m.Groups["body"].Value)).Where(text => text.Length > 0);
    }

    // ---------- Helpers ----------

    private static List<string> NormalizeIngredients(IEnumerable<string> ingredients) => ingredients
        .Select(CleanIngredient)
        .Where(i => i.Length > 0)
        .Select(i => Truncate(i, MaxIngredientLength)!)
        .Distinct()
        .Take(MaxIngredients)
        .ToList();

    // Some recipe plugins put notes in the data as "(, chopped)" or "((optional))".
    private static string CleanIngredient(string ingredient)
    {
        var text = WhitespaceRegex().Replace(ingredient, " ").Trim();
        text = Regex.Replace(text, @"\(\s*[,;]\s*", "(");
        while (text.Contains("((") && text.Contains("))"))
            text = text.Replace("((", "(").Replace("))", ")");
        return Regex.Replace(text, @"\(\s*\)", string.Empty).Replace(" ,", ",").Trim();
    }

    private static string? FormatInstructions(List<Step> steps)
    {
        var builder = new StringBuilder();
        var number = 0;
        foreach (var step in steps)
        {
            var text = StepNumberRegex().Replace(step.Text, string.Empty).Trim();
            if (text.Length == 0)
                continue;
            if (step.IsHeading)
            {
                if (builder.Length > 0)
                    builder.Append('\n');
                builder.Append(text).Append('\n');
            }
            else
            {
                builder.Append(++number).Append(". ").Append(text).Append('\n');
            }
        }

        // A lone heading with no steps isn't useful.
        return number == 0 ? null : builder.ToString().TrimEnd();
    }

    /// <summary>Converts an HTML fragment to plain text, keeping line breaks between blocks.</summary>
    private static string CleanHtml(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var text = WebUtility.HtmlDecode(value);
        if (text.Contains('<'))
        {
            text = ScriptStyleRegex().Replace(text, " ");
            text = BlockBreakRegex().Replace(text, "\n");
            text = TagRegex().Replace(text, string.Empty);
            text = WebUtility.HtmlDecode(text);
        }

        var lines = text.Replace("\r", string.Empty).Split('\n')
            .Select(line => WhitespaceRegex().Replace(line, " ").Trim())
            .Where(line => line.Length > 0);
        return string.Join('\n', lines);
    }

    private static IEnumerable<string> SplitLines(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string? PageTitle(string html)
    {
        var match = OgTitleRegex().Match(html);
        if (!match.Success)
            match = TitleRegex().Match(html);
        var title = match.Success ? CleanHtml(match.Groups[1].Value) : null;
        return string.IsNullOrEmpty(title) ? null : title;
    }

    private static string? Truncate(string? value, int max) =>
        value == null ? null : value.Length > max ? value[..max] : value;

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

        var encoding = Encoding.UTF8;
        var charset = response.Content.Headers.ContentType?.CharSet?.Trim('"', '\'');
        if (!string.IsNullOrEmpty(charset))
        {
            try
            {
                encoding = Encoding.GetEncoding(charset);
            }
            catch (ArgumentException)
            {
                // Unknown charset: fall back to UTF-8.
            }
        }

        return encoding.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }

    /// <summary>
    /// Connect callback that refuses loopback/private/link-local targets so a user-supplied
    /// URL (or redirect) can't be used to reach internal services. Tries each public address
    /// (IPv4 first) so a host whose IPv6 address is unreachable still connects.
    /// </summary>
    public static async ValueTask<Stream> ConnectPublicOnlyAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
        if (addresses.Length == 0 || addresses.Any(IsPrivate))
            throw new HttpRequestException("Target address is not allowed.");

        Exception? lastError = null;
        foreach (var address in addresses.OrderBy(a => a.AddressFamily == AddressFamily.InterNetwork ? 0 : 1))
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException ex)
            {
                socket.Dispose();
                lastError = ex;
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        throw new HttpRequestException("Couldn't connect to the recipe site.", lastError);
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

    [GeneratedRegex("<script[^>]*type\\s*=\\s*[\"']?application/ld\\+json[\"']?[^>]*>(.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex JsonLdRegex();

    [GeneratedRegex("<(script|style)\\b.*?</\\1\\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptStyleRegex();

    [GeneratedRegex("<br\\s*/?>|</(p|li|div|h[1-6]|tr)\\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockBreakRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex("[ \\t\\u00A0]+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex("^(?:step\\s*)?\\d{1,3}\\s*[.):-]\\s+", RegexOptions.IgnoreCase)]
    private static partial Regex StepNumberRegex();

    [GeneratedRegex("<meta[^>]+property\\s*=\\s*[\"']og:title[\"'][^>]*content\\s*=\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase)]
    private static partial Regex OgTitleRegex();

    [GeneratedRegex("<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TitleRegex();
}
