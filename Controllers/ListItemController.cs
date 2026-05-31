using Microsoft.AspNetCore.Mvc;
using ListManager.Data;
using ListManager.Models;

namespace ListManager.Controllers;

public class ListItemController : Controller
{
    private readonly ApplicationDbContext _context;

    public ListItemController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public IActionResult Create(int listID, ListItem item)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction("Manage", "Lists", new { id = listID });
        }

        item.ListID = listID;
        item.CreatedDate = DateTime.Now;
        item.LastUpdatedDate = DateTime.Now;
        item.AddedDate = DateTime.Now;

        _context.ListItems.Add(item);
        _context.SaveChanges();

        return RedirectToAction("Manage", "Lists", new { id = listID });
    }

    public IActionResult Edit(int listID, int listItemID)
    {
        var item = _context.ListItems.FirstOrDefault(li => li.ListID == listID && li.ListItemID == listItemID);
        if (item == null)
        {
            return NotFound();
        }

        ViewBag.ListID = listID;
        return View(item);
    }

    [HttpPost]
    public IActionResult Edit(int listID, ListItem updatedItem)
    {
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

        return RedirectToAction("Manage", "Lists", new { id = listID });
    }

    [HttpPost]
    public IActionResult Delete(int listID, int listItemID)
    {
        var item = _context.ListItems.FirstOrDefault(li => li.ListID == listID && li.ListItemID == listItemID);
        if (item != null)
        {
            _context.ListItems.Remove(item);
            _context.SaveChanges();
        }

        return RedirectToAction("Manage", "Lists", new { id = listID });
    }

    [HttpPost]
    public IActionResult Toggle(int listID, int listItemID)
    {
        var item = _context.ListItems.FirstOrDefault(li => li.ListID == listID && li.ListItemID == listItemID);
        if (item != null)
        {
            item.IsCompleted = !item.IsCompleted;
            item.LastUpdatedDate = DateTime.Now;
            _context.SaveChanges();
        }

        return RedirectToAction("Manage", "Lists", new { id = listID });
    }

}