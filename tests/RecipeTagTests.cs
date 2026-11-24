using ListManager.Models;
using Xunit;

namespace ListManager.Tests;

public class RecipeTagTests
{
    [Fact]
    public void Parse_SplitsOnCommasTrimsAndStripsHashes()
    {
        var tags = RecipeTag.Parse(" Chicken, #Mexican ,, Weeknight dinner ");

        Assert.Equal(new[] { "Chicken", "Mexican", "Weeknight dinner" }, tags);
    }

    [Fact]
    public void Parse_RemovesCaseInsensitiveDuplicates()
    {
        var tags = RecipeTag.Parse("Chicken, chicken, CHICKEN, Beef");

        Assert.Equal(new[] { "Chicken", "Beef" }, tags);
    }

    [Fact]
    public void Parse_TruncatesLongTags()
    {
        var tags = RecipeTag.Parse(new string('a', 80));

        Assert.Equal(RecipeTag.MaxLength, Assert.Single(tags).Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" , # , ")]
    public void Parse_ReturnsEmptyForBlankInput(string input)
    {
        Assert.Empty(RecipeTag.Parse(input));
    }
}
