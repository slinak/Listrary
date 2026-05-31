using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ListManager.Models;

namespace ListManager.Controllers;

public class ListsController : Controller
{
    private static int NextListID = 1;
    public static List<ListManager.Models.List> Lists = new List<ListManager.Models.List>();
    public static List<ListItem> ListItems = new List<ListItem>();
    private readonly ILogger<ListsController> _logger;

    public ListsController(ILogger<ListsController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var listViewModels = Lists.Select(list => new ListViewItemModel
        {
            ListModel = list,
            ItemCount = ListItemController.GetListItemsByListID(list.ListID).Count
        }).ToList();
        return View(listViewModels);
    }

    public IActionResult Create()
    {
        return View(new ListManager.Models.List());
    }

    public IActionResult Edit(int id)
    {
        var list = Lists.FirstOrDefault(l => l.ListID == id);
        if (list == null)
        {
            return NotFound();
        }

        return View(list);
    }

    [HttpPost]
    public IActionResult Edit(List newList)
    {
        var list = Lists.FirstOrDefault(l => l.ListID == newList.ListID);
        if (list != null)
        {
            list.ListName = newList.ListName;
            list.ListType = newList.ListType;
            list.LastUpdatedDate = DateTime.Now;
        }
            
        return RedirectToAction("Index");
    }

    public IActionResult Manage(int id)
    {
        var list = Lists.FirstOrDefault(l => l.ListID == id);
        if (list == null)
        {
            return NotFound();
        }

        var viewModel = new ListItemManagementViewModel
        {
            ListModel = list,
            Items = ListItemController.GetListItemsByListID(id)
        };

        return View(viewModel);
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var list = Lists.FirstOrDefault(l => l.ListID == id);
        if (list != null)
        {
            Lists.Remove(list);
            // Also remove all items associated with this list
            var items = ListItemController.GetListItemsByListID(id);
            foreach (var item in items)
            {
                ListItemController.DeleteListItemFromList(id, item.ListItemID);
            }
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Create(List list)
    {
        var newList = new ListManager.Models.List
        {
            ListID = NextListID++,
            ListName = list?.ListName?.Trim() ?? string.Empty,
            ListType = list?.ListType,
            CreatedDate = DateTime.Now,
            LastUpdatedDate = DateTime.Now
        };

        if (string.IsNullOrWhiteSpace(newList.ListName))
        {
            newList.ListName = Request.Form["ListName"].ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(newList.ListName))
            {
                newList.ListName = Request.Form["list.ListName"].ToString()?.Trim();
            }
        }

        Lists.Add(newList);
        return RedirectToAction("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
