using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ListManager.Models;

namespace ListManager.Controllers;

public class ListItemController : Controller
{
    public static List<ListItem> ListItems = new List<ListItem>();
    public IActionResult Create(ListItem item)
    {
        ListItems.Add(item);
        return View();
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
}