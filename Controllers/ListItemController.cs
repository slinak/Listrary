using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using ListManager;
using ListManager.Data;
using ListManager.Models;

namespace ListManager.Controllers;

[Authorize]
public class ListItemController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ListItemController(ApplicationDbContext context, IStringLocalizer<SharedResource> localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private bool UserOwnsList(int listID)
    {
        return _context.Lists.Any(l => l.ListID == listID && l.OwnerID == CurrentUserId);
    }

    [HttpGet]
    private ListShare? FindShare(int listID, string? shareToken)
    {
        if (!string.IsNullOrWhiteSpace(shareToken))
        {
            var share = _context.ListShares.FirstOrDefault(s => s.ListID == listID && s.Token == shareToken);
            if (share != null)
            {
                if (share.RecipientID == null)
                {
                    share.RecipientID = CurrentUserId;
                    _context.SaveChanges();
                }

                return share;
            }
        }

        return _context.ListShares.FirstOrDefault(s => s.ListID == listID && s.RecipientID == CurrentUserId);
    }

    private bool HasEditAccess(int listID, string? shareToken)
    {
        if (UserOwnsList(listID))
            return true;

        var share = FindShare(listID, shareToken);
        return share != null && share.Permission == ListSharePermission.Edit;
    }

    private static string NormalizeWhitespace(string value)
    {
        return Regex.Replace(value.Trim(), "\\s+", " ");
    }

    private static string NormalizeItemName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return NormalizeWhitespace(value).ToLowerInvariant();
    }

    private static string NormalizeStoreLocationKey(string value)
    {
        var canonical = CanonicalizeStoreLocation(value);
        return Regex.Replace(canonical.ToLowerInvariant(), "[^a-z0-9]+", " ").Trim();
    }

    private static string CanonicalizeStoreLocation(string? value)
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

    private StoreLocationOption? FindSimilarStoreLocationOption(int listID, string normalizedName)
    {
        var options = _context.StoreLocationOptions.Where(option => option.ListID == listID).ToList();
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

    private (StoreLocationOption? option, string? warning, string? error) ResolveStoreLocationOption(int listID, string? rawStoreLocation, bool strictStoreLocations)
    {
        var canonicalStoreLocation = CanonicalizeStoreLocation(rawStoreLocation);
        if (string.IsNullOrWhiteSpace(canonicalStoreLocation))
            return (null, null, null);

        var normalizedName = NormalizeStoreLocationKey(canonicalStoreLocation);
        var existingOption = _context.StoreLocationOptions.FirstOrDefault(option => option.ListID == listID && option.NormalizedName == normalizedName);
        if (existingOption != null)
        {
            existingOption.LastUsedDate = DateTime.Now;
            return (existingOption, null, null);
        }

        var hasAnyOption = _context.StoreLocationOptions.Any(option => option.ListID == listID);
        if (strictStoreLocations && hasAnyOption)
            return (null, null, _localizer["StoreLocations.StrictError"]);

        var similarOption = FindSimilarStoreLocationOption(listID, normalizedName);
        var warning = similarOption == null
            ? null
            : _localizer["StoreLocations.SimilarWarning", similarOption.Name, canonicalStoreLocation].Value;

        var newOption = new StoreLocationOption
        {
            ListID = listID,
            Name = canonicalStoreLocation,
            NormalizedName = normalizedName,
            CreatedDate = DateTime.Now,
            LastUsedDate = DateTime.Now
        };
        _context.StoreLocationOptions.Add(newOption);

        return (newOption, warning, null);
    }

    private void LearnItemLocationPreference(int listID, string? itemName, int storeLocationOptionID)
    {
        var normalizedItemName = NormalizeItemName(itemName);
        if (string.IsNullOrWhiteSpace(normalizedItemName))
            return;

        var preference = _context.ItemLocationPreferences.FirstOrDefault(pref => pref.ListID == listID && pref.ItemNameNormalized == normalizedItemName);
        if (preference == null)
        {
            _context.ItemLocationPreferences.Add(new ItemLocationPreference
            {
                ListID = listID,
                ItemNameNormalized = normalizedItemName,
                StoreLocationOptionID = storeLocationOptionID,
                UsageCount = 1,
                LastUsedDate = DateTime.Now
            });
            return;
        }

        preference.StoreLocationOptionID = storeLocationOptionID;
        preference.UsageCount += 1;
        preference.LastUsedDate = DateTime.Now;
    }

    [HttpPost]
    public IActionResult Create(int listID, ListItem item, string? shareToken)
    {
        if (!HasEditAccess(listID, shareToken))
            return NotFound();

        if (!ModelState.IsValid)
            return RedirectToAction("Manage", "Lists", new { id = listID, shareToken });

        var list = _context.Lists.FirstOrDefault(l => l.ListID == listID);
        if (list == null)
            return NotFound();

        var locationResolution = ResolveStoreLocationOption(listID, item.StoreLocation, list.StrictStoreLocations);
        if (!string.IsNullOrWhiteSpace(locationResolution.error))
        {
            TempData["StoreLocationError"] = locationResolution.error;
            return RedirectToAction("Manage", "Lists", new { id = listID, shareToken });
        }

        if (!string.IsNullOrWhiteSpace(locationResolution.warning))
            TempData["StoreLocationWarning"] = locationResolution.warning;

        item.ListID = listID;
        item.CreatedDate = DateTime.Now;
        item.LastUpdatedDate = DateTime.Now;
        item.AddedDate = DateTime.Now;
        item.StoreLocationOption = locationResolution.option;
        item.StoreLocation = locationResolution.option?.Name;

        _context.ListItems.Add(item);
        _context.SaveChanges();

        if (locationResolution.option?.StoreLocationOptionID > 0)
        {
            LearnItemLocationPreference(listID, item.ItemName, locationResolution.option.StoreLocationOptionID);
            _context.SaveChanges();
        }

        return RedirectToAction("Manage", "Lists", new { id = listID, shareToken });
    }

    public IActionResult Edit(int listID, int listItemID)
    {
        if (!UserOwnsList(listID))
            return NotFound();

        var list = _context.Lists.FirstOrDefault(l => l.ListID == listID);
        if (list == null)
            return NotFound();

        var item = _context.ListItems.FirstOrDefault(li => li.ListID == listID && li.ListItemID == listItemID);
        if (item == null)
            return NotFound();

        ViewBag.ListID = listID;
        ViewBag.StrictStoreLocations = list.StrictStoreLocations;
        return View(item);
    }

    [HttpPost]
    public IActionResult Edit(int listID, ListItem updatedItem, string? shareToken)
    {
        if (!HasEditAccess(listID, shareToken))
            return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.ListID = listID;
            ViewBag.StrictStoreLocations = _context.Lists.Where(l => l.ListID == listID).Select(l => l.StrictStoreLocations).FirstOrDefault();
            return View(updatedItem);
        }

        var list = _context.Lists.FirstOrDefault(l => l.ListID == listID);
        if (list == null)
            return NotFound();

        var locationResolution = ResolveStoreLocationOption(listID, updatedItem.StoreLocation, list.StrictStoreLocations);
        if (!string.IsNullOrWhiteSpace(locationResolution.error))
        {
            ModelState.AddModelError(nameof(updatedItem.StoreLocation), locationResolution.error);
            ViewBag.ListID = listID;
            ViewBag.StrictStoreLocations = list.StrictStoreLocations;
            return View(updatedItem);
        }

        if (!string.IsNullOrWhiteSpace(locationResolution.warning))
            TempData["StoreLocationWarning"] = locationResolution.warning;

        var item = _context.ListItems.FirstOrDefault(li => li.ListID == listID && li.ListItemID == updatedItem.ListItemID);
        if (item != null)
        {
            item.ItemName = updatedItem.ItemName;
            item.ItemDetails = updatedItem.ItemDetails;
            item.StoreLocationOption = locationResolution.option;
            item.StoreLocation = locationResolution.option?.Name;
            item.IsCompleted = updatedItem.IsCompleted;
            item.LastUpdatedDate = DateTime.Now;
            _context.SaveChanges();

            if (locationResolution.option?.StoreLocationOptionID > 0)
            {
                LearnItemLocationPreference(listID, item.ItemName, locationResolution.option.StoreLocationOptionID);
                _context.SaveChanges();
            }
        }

        return RedirectToAction("Manage", "Lists", new { id = listID, shareToken });
    }

    [HttpPost]
    public IActionResult Delete(int listID, int listItemID, string? shareToken)
    {
        if (!HasEditAccess(listID, shareToken))
            return NotFound();

        var item = _context.ListItems.FirstOrDefault(li => li.ListID == listID && li.ListItemID == listItemID);
        if (item != null)
        {
            item.IsDeleted = true;
            _context.ListItems.Remove(item);
            _context.SaveChanges();
        }

        return RedirectToAction("Manage", "Lists", new { id = listID, shareToken });
    }

    [HttpPost]
    public IActionResult Toggle(int listID, int listItemID, string? shareToken)
    {
        if (!HasEditAccess(listID, shareToken))
            return NotFound();

        var item = _context.ListItems.FirstOrDefault(li => li.ListID == listID && li.ListItemID == listItemID);
        if (item != null)
        {
            item.IsCompleted = !item.IsCompleted;
            item.LastUpdatedDate = DateTime.Now;

            // If marking as complete, move to bottom
            if (item.IsCompleted)
            {
                var maxSortOrder = _context.ListItems
                    .Where(li => li.ListID == listID && !li.IsDeleted)
                    .Max(li => (int?)li.SortOrder) ?? 0;
                item.SortOrder = maxSortOrder + 1;
            }

            _context.SaveChanges();
        }

        return RedirectToAction("Manage", "Lists", new { id = listID, shareToken });
    }

    [HttpPost]
    public IActionResult MarkAllIncomplete(int listID, string? shareToken)
    {
        if (!HasEditAccess(listID, shareToken))
            return NotFound();

        var completedItems = _context.ListItems
            .Where(li => li.ListID == listID && !li.IsDeleted && li.IsCompleted)
            .ToList();

        if (completedItems.Any())
        {
            var updatedAt = DateTime.Now;
            foreach (var item in completedItems)
            {
                item.IsCompleted = false;
                item.LastUpdatedDate = updatedAt;
            }

            _context.SaveChanges();
        }

        return RedirectToAction("Manage", "Lists", new { id = listID, shareToken });
    }

    [HttpGet]
    public IActionResult GetItemNameSuggestions(int listID, string? query, string? shareToken)
    {
        if (!HasEditAccess(listID, shareToken))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(query) || query.Length < 1)
            return Json(new List<string>());

        var suggestions = _context.ListItems
            .Where(li => li.ListID == listID
                && !li.IsDeleted
                && li.ItemName != null
                && li.ItemName.ToLower().Contains(query.ToLower()))
            .Select(li => li.ItemName)
            .Distinct()
            .OrderBy(name => name)
            .Take(10)
            .ToList();

        return Json(suggestions);
    }

    [HttpGet]
    public IActionResult GetStoreLocationSuggestions(int listID, string? itemName, string? query, string? shareToken)
    {
        if (!HasEditAccess(listID, shareToken))
            return Unauthorized();

        var normalizedItemName = NormalizeItemName(itemName);
        var normalizedQuery = query?.Trim().ToLower() ?? string.Empty;

        var optionsQuery = _context.StoreLocationOptions.Where(option => option.ListID == listID);
        if (!string.IsNullOrWhiteSpace(normalizedQuery))
            optionsQuery = optionsQuery.Where(option => option.Name.ToLower().Contains(normalizedQuery));

        var preferredOptionUsage = string.IsNullOrWhiteSpace(normalizedItemName)
            ? new Dictionary<int, int>()
            : _context.ItemLocationPreferences
                .Where(preference => preference.ListID == listID && preference.ItemNameNormalized == normalizedItemName)
                .ToDictionary(preference => preference.StoreLocationOptionID, preference => preference.UsageCount);

        var suggestions = optionsQuery
            .ToList()
            .OrderByDescending(option => preferredOptionUsage.TryGetValue(option.StoreLocationOptionID, out var usage) ? usage : 0)
            .ThenByDescending(option => option.LastUsedDate)
            .ThenBy(option => option.Name)
            .Select(option => option.Name)
            .Take(10)
            .ToList();

        return Json(suggestions);
    }
}
