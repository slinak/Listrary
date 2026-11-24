namespace ListManager.Services;

public record RecipeImportResult(string? Title, IReadOnlyList<string> Ingredients, string? Instructions, string? Error)
{
    public bool Succeeded => Error == null;

    public static RecipeImportResult Failure(string error) => new(null, Array.Empty<string>(), null, error);
}

/// <summary>
/// Extracts a recipe title, ingredient list and instructions from a recipe web page.
/// Screenshots are read in the browser (see wwwroot/js/recipe-ocr.js).
/// </summary>
public interface IRecipeImportService
{
    Task<RecipeImportResult> ImportFromUrlAsync(string? url, CancellationToken cancellationToken = default);
}
