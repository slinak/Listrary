using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ListManager.Data;
using ListManager.Models;

namespace ListManager.Controllers;

[Authorize]
public class ListsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ListsController> _logger;

    public ListsController(ApplicationDbContext context, ILogger<ListsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public IActionResult Index()
    {
        var userId = CurrentUserId;
        var listViewModels = _context.Lists
            .Include(list => list.Items)
            .Where(list => list.OwnerId == userId)
            .OrderBy(list => list.CreatedDate)
            .Select(list => new ListViewItemModel
            {
                ListModel = list,
                ItemCount = list.Items.Count
            })
            .ToList();

        return View(listViewModels);
    }

    public IActionResult Create()
    {
        var list = new ListManager.Models.List { ListType = ListType.Grocery };
        return View(list);
    }

    [HttpPost]
    public IActionResult Create(List list)
    {
        if (!ModelState.IsValid)
        {
            return View(list);
        }

        var newList = new ListManager.Models.List
        {
            ListName = list.ListName?.Trim() ?? string.Empty,
            OwnerId = CurrentUserId,
            ListType = list.ListType,
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

        _context.Lists.Add(newList);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }

    public IActionResult Edit(int id)
    {
        var list = _context.Lists.FirstOrDefault(l => l.ListID == id && l.OwnerId == CurrentUserId);
        if (list == null)
        {
            return NotFound();
        }

        return View(list);
    }

    [HttpPost]
    public IActionResult Edit(List newList)
    {
        if (!ModelState.IsValid)
        {
            return View(newList);
        }

        var list = _context.Lists.FirstOrDefault(l => l.ListID == newList.ListID && l.OwnerId == CurrentUserId);
        if (list != null)
        {
            list.ListName = newList.ListName?.Trim();
            list.ListType = newList.ListType;
            list.LastUpdatedDate = DateTime.Now;
            _context.SaveChanges();
        }

        return RedirectToAction("Index");
    }

    public IActionResult Manage(int id)
    {
        var list = _context.Lists
            .Include(l => l.Items)
            .FirstOrDefault(l => l.ListID == id && l.OwnerId == CurrentUserId);

        if (list == null)
        {
            return NotFound();
        }

        var viewModel = new ListItemManagementViewModel
        {
            ListModel = list,
            Items = list.Items.OrderBy(item => item.CreatedDate).ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var list = _context.Lists.FirstOrDefault(l => l.ListID == id && l.OwnerId == CurrentUserId);
        if (list != null)
        {
            _context.Lists.Remove(list);
            _context.SaveChanges();
        }

        return RedirectToAction("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
