using System.Text.RegularExpressions;
using ListManager.Data;
using ListManager.Models;
using Microsoft.Extensions.Localization;

namespace ListManager.Services;

public class ListItemService : IListItemService
{
    private readonly ApplicationDbContext _context;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ListItemService(ApplicationDbContext context, IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public List<ListItem> GetManageViewOrderedItems(IEnumerable<ListItem> items)
    {
        return items
            .OrderBy(item => item.IsCompleted)
            .ThenBy(item => item.IsCompleted ? 0 : (string.IsNullOrWhiteSpace(item.StoreLocation) ? 1 : 0))
            .ThenBy(item => item.IsCompleted ? string.Empty : item.StoreLocation?.Trim(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.SortOrder)
            .ToList();
    }

    private static string NormalizeWhitespace(string value)
    {
        return Regex.Replace(value.Trim(), "\\s+", " ");
    }

    public string NormalizeItemName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return NormalizeWhitespace(value).ToLowerInvariant();
    }

    private static string NormalizeStoreLocationKey(string value)
    {
        var canonical = CanonicalizeStoreLocationStatic(value);
        return Regex.Replace(canonical.ToLowerInvariant(), "[^a-z0-9]+", " ").Trim();
    }

    public string CanonicalizeStoreLocation(string? value) => CanonicalizeStoreLocationStatic(value);

    private static string CanonicalizeStoreLocationStatic(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = NormalizeWhitespace(value)
            .Trim(',', ';', '.', ':')
            .ToLowerInvariant();

        var aliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["produce section"] = "Produce",
            ["vegetables"] = "Produce",
            ["veggies"] = "Produce",
            ["frozen foods"] = "Frozen",
            ["frozen section"] = "Frozen",
            ["checkout"] = "Checkout",
            ["cashier"] = "Checkout"
        };

        if (aliasMap.TryGetValue(normalized, out var aliasCanonical))
            return aliasCanonical;

        var aisleMatch = Regex.Match(normalized, "^aisle\\s*#?\\s*([a-z0-9-]+)$", RegexOptions.IgnoreCase);
        if (aisleMatch.Success)
            return $"Aisle {aisleMatch.Groups[1].Value.ToUpperInvariant()}";

        return string.Join(' ', normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(token => char.ToUpperInvariant(token[0]) + token.Substring(1)));
    }

    private static double GetStringSimilarity(string left, string right)
    {
        if (left == right)
            return 1.0;

        if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
            return 0.0;

        var maxLength = Math.Max(left.Length, right.Length);
        return maxLength == 0 ? 1.0 : 1.0 - (double)LevenshteinDistance(left, right) / maxLength;
    }

    private static int LevenshteinDistance(string left, string right)
    {
        var costs = new int[right.Length + 1];

        for (var j = 0; j <= right.Length; j++)
            costs[j] = j;

        for (var i = 1; i <= left.Length; i++)
        {
            var previousCost = costs[0];
            costs[0] = i;

            for (var j = 1; j <= right.Length; j++)
            {
                var currentCost = costs[j];
                var substitutionCost = left[i - 1] == right[j - 1] ? 0 : 1;
                costs[j] = Math.Min(
                    Math.Min(costs[j] + 1, costs[j - 1] + 1),
                    previousCost + substitutionCost);
                previousCost = currentCost;
            }
        }

        return costs[right.Length];
    }

    private StoreLocationOption? FindSimilarStoreLocationOption(int listId, string normalizedName)
    {
        var options = _context.StoreLocationOptions.Where(option => option.ListID == listId).ToList();
        if (!options.Any())
            return null;

        return options
            .Select(option => new
            {
                Option = option,
                Similarity = GetStringSimilarity(option.NormalizedName, normalizedName)
            })
            .Where(candidate => candidate.Similarity >= 0.75)
            .OrderByDescending(candidate => candidate.Similarity)
            .ThenBy(candidate => candidate.Option.Name)
            .Select(candidate => candidate.Option)
            .FirstOrDefault();
    }

    public (StoreLocationOption? option, string? warning, string? error) ResolveStoreLocationOption(int listId, string? rawStoreLocation, bool strictStoreLocations)
    {
        var canonicalStoreLocation = CanonicalizeStoreLocationStatic(rawStoreLocation);
        if (string.IsNullOrWhiteSpace(canonicalStoreLocation))
            return (null, null, null);

        var normalizedName = NormalizeStoreLocationKey(canonicalStoreLocation);
        var existingOption = _context.StoreLocationOptions.FirstOrDefault(option => option.ListID == listId && option.NormalizedName == normalizedName);
        if (existingOption != null)
        {
            existingOption.LastUsedDate = DateTime.Now;
            return (existingOption, null, null);
        }

        var hasAnyOption = _context.StoreLocationOptions.Any(option => option.ListID == listId);
        if (strictStoreLocations && hasAnyOption)
            return (null, null, _localizer["StoreLocations.StrictError"]);

        var similarOption = FindSimilarStoreLocationOption(listId, normalizedName);
        var warning = similarOption == null
            ? null
            : _localizer["StoreLocations.SimilarWarning", similarOption.Name, canonicalStoreLocation].Value;

        var newOption = new StoreLocationOption
        {
            ListID = listId,
            Name = canonicalStoreLocation,
            NormalizedName = normalizedName,
            CreatedDate = DateTime.Now,
            LastUsedDate = DateTime.Now
        };
        _context.StoreLocationOptions.Add(newOption);

        return (newOption, warning, null);
    }

    public void LearnItemLocationPreference(int listId, string? itemName, int storeLocationOptionId)
    {
        var normalizedItemName = NormalizeItemName(itemName);
        if (string.IsNullOrWhiteSpace(normalizedItemName))
            return;

        var preference = _context.ItemLocationPreferences.FirstOrDefault(pref => pref.ListID == listId && pref.ItemNameNormalized == normalizedItemName);
        if (preference == null)
        {
            _context.ItemLocationPreferences.Add(new ItemLocationPreference
            {
                ListID = listId,
                ItemNameNormalized = normalizedItemName,
                StoreLocationOptionID = storeLocationOptionId,
                UsageCount = 1,
                LastUsedDate = DateTime.Now
            });
            return;
        }

        preference.StoreLocationOptionID = storeLocationOptionId;
        preference.UsageCount += 1;
        preference.LastUsedDate = DateTime.Now;
    }

    public List<string> GetItemNameSuggestions(int listId, string? query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 1)
            return new List<string>();

        return _context.ListItems
            .Where(li => li.ListID == listId
                && !li.IsDeleted
                && li.ItemName != null
                && li.ItemName.ToLower().Contains(query.ToLower()))
            .Select(li => li.ItemName!)
            .Distinct()
            .OrderBy(name => name)
            .Take(10)
            .ToList();
    }

    public List<string> GetStoreLocationSuggestions(int listId, string? itemName, string? query)
    {
        var normalizedItemName = NormalizeItemName(itemName);
        var normalizedQuery = query?.Trim().ToLower() ?? string.Empty;

        var optionsQuery = _context.StoreLocationOptions.Where(option => option.ListID == listId);
        if (!string.IsNullOrWhiteSpace(normalizedQuery))
            optionsQuery = optionsQuery.Where(option => option.Name.ToLower().Contains(normalizedQuery));

        var preferredOptionUsage = string.IsNullOrWhiteSpace(normalizedItemName)
            ? new Dictionary<int, int>()
            : _context.ItemLocationPreferences
                .Where(preference => preference.ListID == listId && preference.ItemNameNormalized == normalizedItemName)
                .ToDictionary(preference => preference.StoreLocationOptionID, preference => preference.UsageCount);

        return optionsQuery
            .ToList()
            .OrderByDescending(option => preferredOptionUsage.TryGetValue(option.StoreLocationOptionID, out var usage) ? usage : 0)
            .ThenByDescending(option => option.LastUsedDate)
            .ThenBy(option => option.Name)
            .Select(option => option.Name)
            .Take(10)
            .ToList();
    }
}
