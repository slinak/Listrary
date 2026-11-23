using System.Text.RegularExpressions;

namespace ListManager.Services;

/// <summary>
/// Splits free-form recipe instructions into individual steps.
/// A new step starts at each non-blank line, at a numbered or bulleted marker
/// ("1.", "2)", "Step 3:", "- ", "•"), or at a sentence boundary when the text is a single unbroken paragraph.
/// </summary>
public static partial class RecipeInstructionParser
{
    [GeneratedRegex(@"^\s*(?:step\s+)?(?:\(?\d+[.)]|\(\d+\)|\d+\s*:|[-*•·])\s*", RegexOptions.IgnoreCase)]
    private static partial Regex StepMarker();

    [GeneratedRegex(@"(?<=[.!?])\s+(?=[A-Z])")]
    private static partial Regex SentenceBoundary();

    public static IReadOnlyList<string> Parse(string? instructions)
    {
        if (string.IsNullOrWhiteSpace(instructions))
            return Array.Empty<string>();

        var steps = new List<string>();
        var lines = instructions.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        foreach (var raw in lines)
        {
            var line = StepMarker().Replace(raw, string.Empty, 1).Trim();
            if (line.Length > 0)
                steps.Add(line);
        }

        // A single unbroken paragraph: fall back to splitting on sentences.
        if (steps.Count == 1)
        {
            var sentences = SentenceBoundary().Split(steps[0]).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            if (sentences.Count > 1)
                return sentences;
        }

        return steps;
    }
}
