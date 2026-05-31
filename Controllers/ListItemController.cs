using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ListManager.Models;

namespace ListManager.Controllers;

public class ListItemController : Controller
{
    public static List<ListItem> ListItems = new List<ListItem>();

    [HttpPost]
    public IActionResult Create(int listID, ListItem item)
    {
        item.ListID = listID;
        ListItems.Add(item);
        return RedirectToAction("Manage", "Lists", new { id = listID });
    }

    public static List<ListItem> GetAllListItems()
    {
        return ListItems;
    }
    public static List<ListItem> GetListItemsByListID(int listID)
    {
        return ListItems.Where(li => li.ListID == listID).ToList();
    }

    public static void AddListItemToList(int listID, ListItem item)
    {
        item.ListID = listID;
        ListItems.Add(item);
    }

    public static void DeleteListItemFromList(int listID, int listItemID)
    {
        var item = ListItems.FirstOrDefault(li => li.ListID == listID && li.ListItemID == listItemID);
        if (item != null)
        {
            ListItems.Remove(item);
        }
    }

    public static void UpdateListItem(int listID, int listItemID, ListItem updatedItem)
    {
        var item = ListItems.FirstOrDefault(li => li.ListID == listID && li.ListItemID == listItemID);
        if (item != null)
        {
            item.ItemName = updatedItem.ItemName;
            item.ItemDetails = updatedItem.ItemDetails;
            item.IsCompleted = updatedItem.IsCompleted;
            item.LastUpdatedDate = DateTime.Now;
        }
    }

    public IActionResult Edit(int listID, int listItemID)
    {
        var item = ListItems.FirstOrDefault(li => li.ListID == listID && li.ListItemID == listItemID);
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
        var item = ListItems.FirstOrDefault(li => li.ListID == listID && li.ListItemID == updatedItem.ListItemID);
        if (item != null)
        {
            item.ItemName = updatedItem.ItemName;
            item.ItemDetails = updatedItem.ItemDetails;
            item.IsCompleted = updatedItem.IsCompleted;
            item.LastUpdatedDate = DateTime.Now;
        }

        return RedirectToAction("Manage", "Lists", new { id = listID });
    }

    [HttpPost]
    public IActionResult Delete(int listID, int listItemID)
    {
        DeleteListItemFromList(listID, listItemID);
        return RedirectToAction("Manage", "Lists", new { id = listID });
    }

    [HttpPost]
    public IActionResult Toggle(int listID, int listItemID)
    {
        var item = ListItems.FirstOrDefault(li => li.ListID == listID && li.ListItemID == listItemID);
        if (item != null)
        {
            item.IsCompleted = !item.IsCompleted;
            item.LastUpdatedDate = DateTime.Now;
        }

        return RedirectToAction("Manage", "Lists", new { id = listID });
    }
}