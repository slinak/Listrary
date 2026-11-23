namespace ListManager.Services;

public record RecipeImportResult(string? Title, IReadOnlyList<string> Ingredients, string? Error)
{
    public bool Succeeded => Error == null;

    public static RecipeImportResult Failure(string error) => new(null, Array.Empty<string>(), error);
}

/// <summary>
/// Extracts a recipe title and ingredient list from a recipe source. The URL
/// implementation is the first source; an image/screenshot source can be added later
/// by producing the same <see cref="RecipeImportResult"/>.
/// </summary>
public interface IRecipeImportService
{
    Task<RecipeImportResult> ImportFromUrlAsync(string? url, CancellationToken cancellationToken = default);
}
