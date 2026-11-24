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

public class RecipeImportInstructionTests
{
    [Fact]
    public void ParseRecipe_ReadsHowToStepsAndSections()
    {
        const string html = """
            <script type="application/ld+json">
            {"@context":"https://schema.org","@type":"Recipe","name":"Short Ribs",
             "recipeIngredient":["2 lb short ribs","1 cup red wine"],
             "recipeInstructions":[
               {"@type":"HowToSection","name":"Braise","itemListElement":[
                 {"@type":"HowToStep","text":"Preheat oven to 160&deg;C."},
                 {"@type":"HowToStep","text":"Brown the ribs."}]},
               {"@type":"HowToStep","text":"<p>Serve <b>hot</b>.</p>"}]}
            </script>
            """;

        var result = RecipeImportService.ParseRecipe(html);

        Assert.NotNull(result);
        Assert.Equal(new[] { "2 lb short ribs", "1 cup red wine" }, result!.Ingredients);
        Assert.Equal("Braise\n1. Preheat oven to 160°C.\n2. Brown the ribs.\n3. Serve hot.", result.Instructions);
    }

    [Fact]
    public void ParseRecipe_ReadsInstructionStringWithLineBreaks()
    {
        const string html = """
            <script type="application/ld+json">
            [{"@type":"WebPage"},{"@type":"Recipe","name":"Toast","recipeIngredient":["1 slice bread"],
              "recipeInstructions":"1. Toast the bread.<br>2. Butter it."}]
            </script>
            """;

        var result = RecipeImportService.ParseRecipe(html);

        Assert.Equal("1. Toast the bread.\n2. Butter it.", result!.Instructions);
    }

    [Fact]
    public void ParseRecipe_FallsBackToMicrodata()
    {
        const string html = """
            <html><head><title>Soup | Example</title></head><body>
            <ul><li itemprop="recipeIngredient">1 onion</li><li itemprop="recipeIngredient">2 cups stock</li></ul>
            <ol itemprop="recipeInstructions"><li>Chop onion.</li><li>Simmer in stock.</li></ol>
            </body></html>
            """;

        var result = RecipeImportService.ParseRecipe(html);

        Assert.NotNull(result);
        Assert.Equal("Soup | Example", result!.Title);
        Assert.Equal(new[] { "1 onion", "2 cups stock" }, result.Ingredients);
        Assert.Equal("1. Chop onion.\n2. Simmer in stock.", result.Instructions);
    }
}

public class RecipeImportIngredientCleanupTests
{
    [Fact]
    public void ParseRecipe_CleansPluginNoteFormatting()
    {
        const string html = """
            <script type="application/ld+json">
            {"@type":"Recipe","name":"Ribs","recipeIngredient":["3 garlic cloves (, crushed)","2 sprigs thyme ((optional))","1 onion ()"]}
            </script>
            """;

        var result = RecipeImportService.ParseRecipe(html);

        Assert.Equal(new[] { "3 garlic cloves (crushed)", "2 sprigs thyme (optional)", "1 onion" }, result!.Ingredients);
        Assert.Null(result.Instructions);
    }
}
