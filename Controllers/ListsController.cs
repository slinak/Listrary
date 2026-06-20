using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

    private SelectList GetCircleSelectList()
    {
        var circles = _context.Circles
            .Include(c => c.Members)
            .Where(c => c.OwnerID == CurrentUserId || c.Members.Any(m => m.UserID == CurrentUserId))
            .OrderBy(c => c.Name)
            .ToList();

        return new SelectList(circles, nameof(Models.Circle.CircleID), nameof(Models.Circle.Name));
    }

    private bool UserOwnsList(int listId)
    {
        return _context.Lists.Any(list => list.ListID == listId && list.OwnerID == CurrentUserId);
    }

    private ListShare? FindShare(int listId, string? shareToken)
    {
        if (string.IsNullOrEmpty(shareToken))
            return _context.ListShares.FirstOrDefault(share => share.ListID == listId && share.RecipientID == CurrentUserId);

        var share = _context.ListShares.FirstOrDefault(share => share.ListID == listId && share.Token == shareToken);
        if (share != null && share.RecipientID == null)
        {
            share.RecipientID = CurrentUserId;
            _context.SaveChanges();
        }

        return share?.RecipientID == CurrentUserId ? share : null;
    }

    private bool HasViewAccess(int listId, string? shareToken)
    {
        if (UserOwnsList(listId))
            return true;
        // Check circle membership: if the list belongs to a circle and the current user is a member, allow view
        var listForCircle = _context.Lists.FirstOrDefault(l => l.ListID == listId);
        if (listForCircle != null && listForCircle.CircleID != null)
        {
            var isMember = _context.CircleMembers.Any(cm => cm.CircleID == listForCircle.CircleID && cm.UserID == CurrentUserId);
            if (isMember)
                return true;
        }

        var share = FindShare(listId, shareToken);
        return share != null;
    }

    private bool HasEditAccess(int listId, string? shareToken)
    {
        if (UserOwnsList(listId))
            return true;

        var share = FindShare(listId, shareToken);
        return share?.Permission == ListSharePermission.Edit;
    }

    private static List<ListItem> GetManageViewOrderedItems(IEnumerable<ListItem> items)
    {
        return items
            .OrderBy(item => item.IsCompleted)
            .ThenBy(item => item.IsCompleted ? 0 : (string.IsNullOrWhiteSpace(item.StoreLocation) ? 1 : 0))
            .ThenBy(item => item.IsCompleted ? string.Empty : item.StoreLocation?.Trim(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.SortOrder)
            .ToList();
    }

    [HttpGet]
    public IActionResult Index()
    {
        var userId = CurrentUserId;

        var circles = _context.Circles
            .Include(c => c.Members)
            .Where(c => c.OwnerID == userId || c.Members.Any(m => m.UserID == userId))
            .OrderBy(c => c.Name)
            .ToList();

        var circleGroups = circles.Select(circle => new CircleListsViewModel
        {
            Circle = circle,
            Lists = _context.Lists
                .Include(list => list.Items)
                .Include(list => list.Shares)
                .Include(list => list.Circle)
                .Where(list => list.CircleID == circle.CircleID)
                .OrderBy(list => list.CreatedDate)
                .Select(list => new ListViewItemModel
                {
                    ListModel = list,
                    ItemCount = list.Items.Count
                })
                .ToList()
        }).ToList();

        var circleListIds = circleGroups.SelectMany(group => group.Lists.Select(item => item.ListModel!.ListID)).ToHashSet();

        var otherLists = _context.Lists
            .Include(list => list.Items)
            .Include(list => list.Shares)
            .Where(list => !list.CircleID.HasValue
                && (list.OwnerID == userId || list.Shares.Any(share => share.RecipientID == userId)))
            .OrderBy(list => list.CreatedDate)
            .Select(list => new ListViewItemModel
            {
                ListModel = list,
                ItemCount = list.Items.Count
            })
            .ToList();

        var viewModel = new ListsIndexViewModel
        {
            CircleGroups = circleGroups,
            OtherLists = otherLists
        };

        return View(viewModel);
    }

    // Show all lists that belong to a specific circle (only available to circle owner or members)
    [HttpGet]
    public IActionResult Circle(int id)
    {
        var circle = _context.Circles
            .Include(c => c.Members)
            .FirstOrDefault(c => c.CircleID == id);

        if (circle == null)
            return NotFound();

        var userId = CurrentUserId;
        var isMemberOrOwner = circle.OwnerID == userId || circle.Members.Any(m => m.UserID == userId);
        if (!isMemberOrOwner)
            return NotFound();

        var listViewModels = _context.Lists
            .Include(l => l.Items)
            .Include(l => l.Shares)
            .Include(l => l.Circle)
            .Where(l => l.CircleID == id)
            .OrderBy(l => l.CreatedDate)
            .Select(l => new ListViewItemModel
            {
                ListModel = l,
                ItemCount = l.Items.Count
            })
            .ToList();

        ViewData["CircleName"] = circle.Name;
        ViewData["CircleId"] = id;

        return View("Circle", listViewModels);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.CircleOptions = GetCircleSelectList();
        return View(new ListManager.Models.List { ListType = ListType.Shopping });
    }

    [HttpPost]
    public IActionResult Create(List list)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.CircleOptions = GetCircleSelectList();
            return View(list);
        }

        var newList = new ListManager.Models.List
        {
            ListName = list.ListName?.Trim() ?? string.Empty,
            OwnerID = CurrentUserId,
            CircleID = list.CircleID,
            ListType = list.ListType,
            CreatedDate = DateTime.Now,
            LastUpdatedDate = DateTime.Now
        };

        if (string.IsNullOrWhiteSpace(newList.ListName))
        {
            newList.ListName = Request.Form["ListName"].ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(newList.ListName))
                newList.ListName = Request.Form["list.ListName"].ToString()?.Trim();
        }

        _context.Lists.Add(newList);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        if (!UserOwnsList(id))
            return NotFound();

        var list = _context.Lists.FirstOrDefault(l => l.ListID == id);
        if (list == null)
            return NotFound();

        ViewBag.CircleOptions = GetCircleSelectList();
        return View(list);
    }

    [HttpPost]
    public IActionResult Edit(List newList)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.CircleOptions = GetCircleSelectList();
            return View(newList);
        }

        var list = _context.Lists.FirstOrDefault(l => l.ListID == newList.ListID && l.OwnerID == CurrentUserId);
        if (list != null)
        {
            list.ListName = newList.ListName?.Trim();
            list.CircleID = newList.CircleID;
            list.ListType = newList.ListType;
            list.LastUpdatedDate = DateTime.Now;
            _context.SaveChanges();
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Manage(int id, string? shareToken)
    {
        var list = _context.Lists
            .Include(l => l.Items)
            .Include(l => l.Shares)
            .FirstOrDefault(l => l.ListID == id);

        if (list == null)
            return NotFound();

        if (!HasViewAccess(id, shareToken))
            return NotFound();

        var share = FindShare(id, shareToken);
        var viewModel = new ListItemManagementViewModel
        {
            ListModel = list,
            Items = GetManageViewOrderedItems(list.Items),
            CanEdit = HasEditAccess(id, shareToken),
            ShareToken = share?.Token,
            ActiveShares = list.Shares.ToList(),
            GeneratedShareLink = null
        };

        return View(viewModel);
    }

    [HttpPost]
    public IActionResult Share(int id, ListSharePermission permission)
    {
        if (!UserOwnsList(id))
            return NotFound();

        var share = new ListShare
        {
            ListID = id,
            OwnerID = CurrentUserId,
            Permission = permission,
            Token = Guid.NewGuid().ToString("N"),
            CreatedDate = DateTime.Now
        };

        _context.ListShares.Add(share);
        _context.SaveChanges();

        var shareLink = Url.Action("Manage", "Lists", new { id = id, shareToken = share.Token }, Request.Scheme);

        var list = _context.Lists
            .Include(l => l.Items)
            .Include(l => l.Shares)
            .FirstOrDefault(l => l.ListID == id && l.OwnerID == CurrentUserId);

        if (list == null)
            return NotFound();

        var viewModel = new ListItemManagementViewModel
        {
            ListModel = list,
            Items = GetManageViewOrderedItems(list.Items),
            CanEdit = true,
            ShareToken = share.Token,
            ActiveShares = list.Shares.ToList(),
            GeneratedShareLink = shareLink
        };

        return View("Manage", viewModel);
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var list = _context.Lists.FirstOrDefault(l => l.ListID == id && l.OwnerID == CurrentUserId);
        if (list != null)
        {
            _context.Lists.Remove(list);
            _context.SaveChanges();
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
