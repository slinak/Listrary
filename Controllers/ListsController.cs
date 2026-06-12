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

    private bool UserOwnsList(int listId)
    {
        return _context.Lists.Any(list => list.ListID == listId && list.OwnerId == CurrentUserId);
    }

    private ListShare? FindShare(int listId, string? shareToken)
    {
        if (string.IsNullOrEmpty(shareToken))
            return _context.ListShares.FirstOrDefault(share => share.ListID == listId && share.RecipientId == CurrentUserId);

        var share = _context.ListShares.FirstOrDefault(share => share.ListID == listId && share.Token == shareToken);
        if (share != null && share.RecipientId == null)
        {
            share.RecipientId = CurrentUserId;
            _context.SaveChanges();
        }

        return share?.RecipientId == CurrentUserId ? share : null;
    }

    private bool HasViewAccess(int listId, string? shareToken)
    {
        if (UserOwnsList(listId))
            return true;
        // Check circle membership: if the list belongs to a circle and the current user is a member, allow view
        var listForCircle = _context.Lists.FirstOrDefault(l => l.ListID == listId);
        if (listForCircle != null && listForCircle.CircleId != null)
        {
            var isMember = _context.CircleMembers.Any(cm => cm.CircleId == listForCircle.CircleId && cm.UserId == CurrentUserId);
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

    public IActionResult Index()
    {
        var userId = CurrentUserId;
        var listViewModels = _context.Lists
            .Include(list => list.Items)
            .Include(list => list.Shares)
            .Include(list => list.Circle)
                .ThenInclude(c => c.Members)
            .Where(list => list.OwnerId == userId
                || list.Shares.Any(share => share.RecipientId == userId)
                || (list.Circle != null && list.Circle.Members.Any(m => m.UserId == userId)))
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
        return View(new ListManager.Models.List { ListType = ListType.Grocery });
    }

    [HttpPost]
    public IActionResult Create(List list)
    {
        if (!ModelState.IsValid)
            return View(list);

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
                newList.ListName = Request.Form["list.ListName"].ToString()?.Trim();
        }

        _context.Lists.Add(newList);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }

    public IActionResult Edit(int id)
    {
        if (!UserOwnsList(id))
            return NotFound();

        var list = _context.Lists.FirstOrDefault(l => l.ListID == id);
        if (list == null)
            return NotFound();

        return View(list);
    }

    [HttpPost]
    public IActionResult Edit(List newList)
    {
        if (!ModelState.IsValid)
            return View(newList);

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
            Items = list.Items.OrderBy(item => item.CreatedDate).ToList(),
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
            OwnerId = CurrentUserId,
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
            .FirstOrDefault(l => l.ListID == id && l.OwnerId == CurrentUserId);

        if (list == null)
            return NotFound();

        var viewModel = new ListItemManagementViewModel
        {
            ListModel = list,
            Items = list.Items.OrderBy(item => item.CreatedDate).ToList(),
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
