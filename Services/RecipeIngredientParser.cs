using System.Text.RegularExpressions;

namespace ListManager.Services;

/// <summary>An ingredient line split for display: "1/3 cup (80ml) olive oil, plus more" → Amount "1/3 cup", Name "olive oil", Note "80ml; plus more".</summary>
public sealed record ParsedIngredient(string? Amount, string Name, string? Note);

/// <summary>
/// Splits a free-form ingredient line into amount, name and preparation note for display.
/// The stored text is never changed; when a line can't be split it comes back whole as the Name.
/// </summary>
public static partial class RecipeIngredientParser
{
    // Longest forms first: "1 1/4", "1/2", "1½" / "1.5", "½".
    private const string Number = @"(?:\d+\s+\d+/\d+|\d+/\d+|\d+(?:[.,]\d+)?(?:\s*[½⅓⅔¼¾⅛⅜⅝⅞⅕⅖⅗⅘⅙⅚])?|[½⅓⅔¼¾⅛⅜⅝⅞⅕⅖⅗⅘⅙⅚])";

    private const string Unit = @"(?:(?:small|medium|large|heaping|heaped|level|rounded|scant|generous)\s+)?" +
        @"(?:cups?|c|tablespoons?|tbsps?|tbs|tbl|teaspoons?|tsps?|fluid\s+ounces?|fl\.?\s*oz|ounces?|oz|pounds?|lbs?|" +
        @"kilograms?|kg|grams?|g|milligrams?|mg|millilit(?:er|re)s?|ml|lit(?:er|re)s?|l|quarts?|qts?|pints?|pts?|gallons?|gal|" +
        @"cloves?|cans?|jars?|packages?|pkgs?|sticks?|slices?|pieces?|pinch(?:es)?|dash(?:es)?|handfuls?|bunch(?:es)?|" +
        @"sprigs?|heads?|stalks?|bags?|bottles?|boxes|containers?|sheets?|drops?)\.?";

    // Quantity (with an optional range), then optional "(8-ounce)", unit, "(45ml)" in either order.
    [GeneratedRegex(@"^(?<qty>" + Number + @"(?:\s*(?:-|–|to|or)\s*" + Number + @")?)(?=[\s(]|$)" +
                    @"(?:\s*\((?<p1>[^()]*)\))?" +
                    @"(?:\s+(?<unit>" + Unit + @")(?=[\s,(]|$))?" +
                    @"(?:\s*\((?<p2>[^()]*)\))?\s*",
        RegexOptions.IgnoreCase)]
    private static partial Regex LeadingAmount();

    // Words that start a preparation note after a comma ("grated", "finely chopped", "to taste", "plus more").
    // Adjective lists such as "boneless, skinless chicken" don't match, so they stay in the name.
    [GeneratedRegex(@"^(?:\w+ed|\w+ly|to|plus|or|about|for|at|cut|such|preferably|optional|if|from|into|with|without|as|divided|torn|room|cold|warm|hot|softened|halved|whole|left)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex NoteStart();

    [GeneratedRegex(@"\s*\(([^()]*)\)\s*$")]
    private static partial Regex TrailingParenthetical();

    public static ParsedIngredient Parse(string? text)
    {
        var line = (text ?? string.Empty).Trim();
        if (line.Length == 0)
            return new ParsedIngredient(null, string.Empty, null);

        var notes = new List<string>();
        string? amount = null;
        var rest = line;

        var match = LeadingAmount().Match(line);
        if (match.Success)
        {
            amount = match.Groups["qty"].Value.Trim();
            if (match.Groups["unit"].Success)
                amount += " " + match.Groups["unit"].Value.Trim();
            AddNote(notes, match.Groups["p1"].Value);
            AddNote(notes, match.Groups["p2"].Value);
            rest = line[match.Length..];
        }

        var (name, tail) = SplitAtNote(rest);

        var trailing = TrailingParenthetical().Match(name);
        if (trailing.Success && trailing.Index > 0)
        {
            AddNote(notes, trailing.Groups[1].Value);
            name = name[..trailing.Index];
        }

        AddNote(notes, tail);
        name = name.Trim().TrimEnd(',').Trim();

        // Nothing left to call the ingredient (e.g. "2 cups"): show the line as written.
        if (name.Length == 0)
            return new ParsedIngredient(null, line, null);

        return new ParsedIngredient(amount, name, notes.Count == 0 ? null : string.Join("; ", notes));
    }

    /// <summary>Display form of a section heading: "For the Kebabs" → "Kebabs".</summary>
    public static string FormatSection(string section)
    {
        var trimmed = section.Trim().TrimEnd(':').Trim();
        foreach (var prefix in new[] { "For the ", "For " })
        {
            if (trimmed.Length > prefix.Length && trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var rest = trimmed[prefix.Length..].Trim();
                return char.ToUpper(rest[0]) + rest[1..];
            }
        }
        return trimmed;
    }

    // Splits at the first comma outside parentheses that is followed by a preparation note.
    private static (string Name, string? Note) SplitAtNote(string text)
    {
        var depth = 0;
        for (var i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '(': depth++; break;
                case ')': depth = Math.Max(0, depth - 1); break;
                case ',' when depth == 0:
                    var after = text[(i + 1)..].Trim();
                    if (after.Length > 0 && NoteStart().IsMatch(after))
                        return (text[..i], after);
                    break;
            }
        }
        return (text, null);
    }

    private static void AddNote(List<string> notes, string? note)
    {
        var trimmed = note?.Trim();
        if (!string.IsNullOrEmpty(trimmed))
            notes.Add(trimmed);
    }
}
