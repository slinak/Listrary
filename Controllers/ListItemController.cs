using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ListManager.Data;
using ListManager.Models;

namespace ListManager.Controllers;

[Authorize]
public class ListItemController : Controller
{
    private readonly ApplicationDbContext _context;

    public ListItemController(ApplicationDbContext context)
    {
        _context = context;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private bool UserOwnsList(int listID)
    {
        return _context.Lists.Any(l => l.ListID == listID && l.OwnerId == CurrentUserId);
    }

    private ListShare? FindShare(int listID, string? shareToken)
    {
        if (!string.IsNullOrWhiteSpace(shareToken))
        {
            var share = _context.ListShares.FirstOrDefault(s => s.ListID == listID && s.Token == shareToken);
            if (share != null)
            {
                if (share.RecipientId == null)
                {
                    share.RecipientId = CurrentUserId;
                    _context.SaveChanges();
                }

                return share;
            }
        }

        return _context.ListShares.FirstOrDefault(s => s.ListID == listID && s.RecipientId == CurrentUserId);
    }

    private bool HasEditAccess(int listID, string? shareToken)
    {
        if (UserOwnsList(listID))
        {
            return true;
        }

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

        item.ListID = listID;
        item.CreatedDate = DateTime.Now;
        item.LastUpdatedDate = DateTime.Now;
        item.AddedDate = DateTime.Now;

        _context.ListItems.Add(item);
        _context.SaveChanges();

        return RedirectToAction("Manage", "Lists", new { id = listID, shareToken });
    }

    public IActionResult Edit(int listID, int listItemID)
    {
        if (!UserOwnsList(listID))
            return NotFound();

        var item = _context.ListItems.FirstOrDefault(li => li.ListID == listID && li.ListItemID == listItemID);
        if (item == null)
            return NotFound();

        ViewBag.ListID = listID;
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
            return View(updatedItem);
        }

        var item = _context.ListItems.FirstOrDefault(li => li.ListID == listID && li.ListItemID == updatedItem.ListItemID);
        if (item != null)
        {
            item.ItemName = updatedItem.ItemName;
            item.ItemDetails = updatedItem.ItemDetails;
            item.IsCompleted = updatedItem.IsCompleted;
            item.LastUpdatedDate = DateTime.Now;
            _context.SaveChanges();
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
            _context.SaveChanges();
        }

        return RedirectToAction("Manage", "Lists", new { id = listID, shareToken });
    }
}
