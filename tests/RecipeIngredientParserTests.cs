using ListManager.Services;
using Xunit;

namespace ListManager.Tests;

public class RecipeIngredientParserTests
{
    [Theory]
    [InlineData("3 medium cloves garlic, grated or minced (about 1 tablespoon; 15g)", "3 medium cloves", "garlic", "grated or minced (about 1 tablespoon; 15g)")]
    [InlineData("3 tablespoons (45ml) extra-virgin olive oil", "3 tablespoons", "extra-virgin olive oil", "45ml")]
    [InlineData("1 teaspoon (about 4g) coarse kosher salt", "1 teaspoon", "coarse kosher salt", "about 4g")]
    [InlineData("1/2 teaspoon (about 2g) freshly ground black pepper", "1/2 teaspoon", "freshly ground black pepper", "about 2g")]
    [InlineData("2 pounds (900g) boneless, skinless chicken thighs (or breasts)", "2 pounds", "boneless, skinless chicken thighs", "900g; or breasts")]
    [InlineData("1 pint large cherry tomatoes (about 12 ounces; 340g)", "1 pint", "large cherry tomatoes", "about 12 ounces; 340g")]
    [InlineData("1 1/4 cups fresh basil leaves (about 1 1/4 ounces; 35g)", "1 1/4 cups", "fresh basil leaves", "about 1 1/4 ounces; 35g")]
    [InlineData("1/3 cup (80ml) extra-virgin olive oil, plus more for brushing tomatoes", "1/3 cup", "extra-virgin olive oil", "80ml; plus more for brushing tomatoes")]
    [InlineData("1 small garlic clove, grated (about 1 teaspoon; 4g)", "1", "small garlic clove", "grated (about 1 teaspoon; 4g)")]
    [InlineData("Kosher salt and freshly ground pepper, to taste", null, "Kosher salt and freshly ground pepper", "to taste")]
    [InlineData("1 (8-ounce) package cream cheese, softened", "1 package", "cream cheese", "8-ounce; softened")]
    [InlineData("1-2 tbsp. honey", "1-2 tbsp.", "honey", null)]
    [InlineData("½ cup sugar", "½ cup", "sugar", null)]
    [InlineData("2 large eggs", "2", "large eggs", null)]
    [InlineData("2 cans chickpeas", "2 cans", "chickpeas", null)]
    public void Parse_SplitsAmountNameAndNote(string text, string amount, string name, string note)
    {
        var parsed = RecipeIngredientParser.Parse(text);

        Assert.Equal(amount, parsed.Amount);
        Assert.Equal(name, parsed.Name);
        Assert.Equal(note, parsed.Note);
    }

    [Fact]
    public void Parse_KeepsWholeLineWhenNothingIsLeftForTheName()
    {
        var parsed = RecipeIngredientParser.Parse("2 cups");

        Assert.Null(parsed.Amount);
        Assert.Equal("2 cups", parsed.Name);
        Assert.Null(parsed.Note);
    }

    [Theory]
    [InlineData("For the Kebabs", "Kebabs")]
    [InlineData("for the chimichurri:", "Chimichurri")]
    [InlineData("For Serving", "Serving")]
    [InlineData("Dough", "Dough")]
    [InlineData("Fortified Wine", "Fortified Wine")]
    public void FormatSection_DropsLeadingFor(string section, string expected)
    {
        Assert.Equal(expected, RecipeIngredientParser.FormatSection(section));
    }
}
