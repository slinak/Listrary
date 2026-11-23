using ListManager.Services;
using Xunit;

namespace ListManager.Tests;

public class RecipeInstructionParserTests
{
    [Fact]
    public void Parse_SplitsLinesAndStripsNumberedMarkers()
    {
        var steps = RecipeInstructionParser.Parse("1. Preheat oven\n2) Mix flour\r\nStep 3: Bake");

        Assert.Equal(new[] { "Preheat oven", "Mix flour", "Bake" }, steps);
    }

    [Fact]
    public void Parse_SplitsSentencesWhenTextIsOneParagraph()
    {
        var steps = RecipeInstructionParser.Parse("Preheat the oven to 350.5 degrees. Mix the flour. Bake for 20 minutes!");

        Assert.Equal(new[] { "Preheat the oven to 350.5 degrees.", "Mix the flour.", "Bake for 20 minutes!" }, steps);
    }

    [Fact]
    public void Parse_IgnoresBlankLines()
    {
        var steps = RecipeInstructionParser.Parse("Chop onions\n\n\nFry them");

        Assert.Equal(new[] { "Chop onions", "Fry them" }, steps);
    }

    [Fact]
    public void Parse_ReturnsEmptyForBlankInput()
    {
        Assert.Empty(RecipeInstructionParser.Parse("  \n "));
        Assert.Empty(RecipeInstructionParser.Parse(null));
    }
}
