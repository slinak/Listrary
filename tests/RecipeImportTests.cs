using ListManager.Services;
using Xunit;

namespace ListManager.Tests;

public class RecipeImportTests
{
    [Fact]
    public void ParseRecipe_ReadsIngredientsFromJsonLdGraph()
    {
        const string html = """
            <html><script type="application/ld+json">
            {"@graph":[{"@type":"WebSite"},{"@type":["Recipe"],"name":"Pancakes &amp; Syrup","recipeIngredient":["1 cup flour","2 eggs"]}]}
            </script></html>
            """;

        var result = RecipeImportService.ParseRecipe(html);

        Assert.NotNull(result);
        Assert.Equal("Pancakes & Syrup", result!.Title);
        Assert.Equal(new[] { "1 cup flour", "2 eggs" }, result.Ingredients);
    }

    [Fact]
    public void ParseRecipe_ReturnsNullWithoutRecipe()
    {
        Assert.Null(RecipeImportService.ParseRecipe("<html><body>Hello</body></html>"));
    }
}
