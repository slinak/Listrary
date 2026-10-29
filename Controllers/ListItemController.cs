using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ListManager.Data;
using ListManager.Models;
using ListManager.Services;

namespace ListManager.Controllers;

[Authorize]
public class ListItemController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IListItemService _listItemService;

    public ListItemController(ApplicationDbContext context, IListItemService listItemService)
    {
        _context = context;
        _listItemService = listItemService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private bool UserOwnsList(int listID)
    {
        return _context.Lists.Any(l => l.ListID == listID && l.OwnerID == CurrentUserId);
    }

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

        var shouldUseStoreLocation = list.ListType == ListType.Shopping;
        var rawStoreLocation = shouldUseStoreLocation ? item.StoreLocation : null;

        var locationResolution = _listItemService.ResolveStoreLocationOption(listID, rawStoreLocation, list.StrictStoreLocations);
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
            _listItemService.LearnItemLocationPreference(listID, item.ItemName, locationResolution.option.StoreLocationOptionID);
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
        ViewBag.ShowStoreLocation = list.ListType == ListType.Shopping;
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
            ViewBag.ShowStoreLocation = _context.Lists.Where(l => l.ListID == listID).Select(l => l.ListType == ListType.Shopping).FirstOrDefault();
            return View(updatedItem);
        }

        var list = _context.Lists.FirstOrDefault(l => l.ListID == listID);
        if (list == null)
            return NotFound();

        var shouldUseStoreLocation = list.ListType == ListType.Shopping;
        var rawStoreLocation = shouldUseStoreLocation ? updatedItem.StoreLocation : null;

        var locationResolution = _listItemService.ResolveStoreLocationOption(listID, rawStoreLocation, list.StrictStoreLocations);
        if (!string.IsNullOrWhiteSpace(locationResolution.error))
        {
            ModelState.AddModelError(nameof(updatedItem.StoreLocation), locationResolution.error);
            ViewBag.ListID = listID;
            ViewBag.StrictStoreLocations = list.StrictStoreLocations;
            ViewBag.ShowStoreLocation = list.ListType == ListType.Shopping;
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
                _listItemService.LearnItemLocationPreference(listID, item.ItemName, locationResolution.option.StoreLocationOptionID);
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

        return Json(_listItemService.GetItemNameSuggestions(listID, query));
    }

    [HttpGet]
    public IActionResult GetStoreLocationSuggestions(int listID, string? itemName, string? query, string? shareToken)
    {
        if (!HasEditAccess(listID, shareToken))
            return Unauthorized();

        return Json(_listItemService.GetStoreLocationSuggestions(listID, itemName, query));
    }
}
