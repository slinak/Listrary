using System.Security.Claims;
using ListManager.Data;
using ListManager.Dtos;
using ListManager.Models;
using ListManager.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListManager.Controllers.Api;

/// <summary>
/// JSON REST API for Lists, consumed by the Android client. Mirrors the behavior of
/// <see cref="ListsController"/> but returns DTOs and authenticates via Entra ID JWT
/// bearer tokens instead of the web UI's cookie session.
/// </summary>
[ApiController]
[Route("api/v1/lists")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ListsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IListItemService _listItemService;

    public ListsApiController(ApplicationDbContext context, IListItemService listItemService)
    {
        _context = context;
        _listItemService = listItemService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private bool UserOwnsList(int listId) => _context.Lists.Any(l => l.ListID == listId && l.OwnerID == CurrentUserId);

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

        var listForCircle = _context.Lists.FirstOrDefault(l => l.ListID == listId);
        if (listForCircle != null && listForCircle.CircleID != null)
        {
            var isMember = _context.CircleMembers.Any(cm => cm.CircleID == listForCircle.CircleID && cm.UserID == CurrentUserId);
            if (isMember)
                return true;
        }

        return FindShare(listId, shareToken) != null;
    }

    private bool HasEditAccess(int listId, string? shareToken)
    {
        if (UserOwnsList(listId))
            return true;

        var share = FindShare(listId, shareToken);
        return share?.Permission == ListSharePermission.Edit;
    }

    /// <summary>GET api/v1/lists — lists grouped by circle, plus personal/shared-with-me lists.</summary>
    [HttpGet]
    public IActionResult GetLists()
    {
        var userId = CurrentUserId;

        var circles = _context.Circles
            .Include(c => c.Members)
            .Where(c => c.OwnerID == userId || c.Members.Any(m => m.UserID == userId))
            .OrderBy(c => c.Name)
            .ToList();

        var circleGroups = circles.Select(circle => new
        {
            Circle = new CircleSummaryDto(circle.CircleID, circle.Name),
            Lists = _context.Lists
                .Include(l => l.Items)
                .Include(l => l.Circle)
                .Where(l => l.CircleID == circle.CircleID)
                .OrderBy(l => l.CreatedDate)
                .ToList()
                .Select(l => l.ToSummaryDto(userId, l.Items.Count(i => !i.IsDeleted)))
                .ToList()
        }).ToList();

        var otherLists = _context.Lists
            .Include(l => l.Items)
            .Include(l => l.Shares)
            .Where(l => !l.CircleID.HasValue && (l.OwnerID == userId || l.Shares.Any(s => s.RecipientID == userId)))
            .OrderBy(l => l.CreatedDate)
            .ToList()
            .Select(l => l.ToSummaryDto(userId, l.Items.Count(i => !i.IsDeleted)))
            .ToList();

        return Ok(new { circleGroups, otherLists });
    }

    /// <summary>GET api/v1/lists/{id} — list detail, optionally via a share token (anonymous view/edit).</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public IActionResult GetList(int id, [FromQuery] string? shareToken)
    {
        var list = _context.Lists
            .Include(l => l.Items)
            .Include(l => l.Shares)
            .Include(l => l.Circle)
            .FirstOrDefault(l => l.ListID == id);

        if (list == null)
            return NotFound();

        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
        var publicViewShare = !string.IsNullOrEmpty(shareToken)
            ? list.Shares.FirstOrDefault(s => s.Token == shareToken && s.Permission == ListSharePermission.View)
            : null;

        if (!isAuthenticated)
        {
            if (publicViewShare == null)
                return Unauthorized();

            var orderedItems = _listItemService.GetManageViewOrderedItems(list.Items.Where(i => !i.IsDeleted));
            return Ok(new ListDetailDto(
                list.ToSummaryDto(string.Empty, orderedItems.Count),
                orderedItems.Select(i => i.ToDto()).ToList(),
                false,
                publicViewShare.Token,
                new List<ListShareDto>()));
        }

        if (!HasViewAccess(id, shareToken) && publicViewShare == null)
            return NotFound();

        var share = FindShare(id, shareToken) ?? publicViewShare;
        var items = _listItemService.GetManageViewOrderedItems(list.Items.Where(i => !i.IsDeleted));

        return Ok(new ListDetailDto(
            list.ToSummaryDto(CurrentUserId, items.Count),
            items.Select(i => i.ToDto()).ToList(),
            HasEditAccess(id, shareToken),
            share?.Token,
            list.Shares.Select(s => s.ToDto()).ToList()));
    }

    /// <summary>POST api/v1/lists — create a new list owned by the current user.</summary>
    [HttpPost]
    public IActionResult CreateList([FromBody] ListRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ListName))
            return BadRequest(new { error = "ListName is required." });

        var newList = new List
        {
            ListName = request.ListName.Trim(),
            OwnerID = CurrentUserId,
            CircleID = request.CircleId,
            ListType = request.ListType,
            StrictStoreLocations = request.StrictStoreLocations,
            CreatedDate = DateTime.Now,
            LastUpdatedDate = DateTime.Now
        };

        _context.Lists.Add(newList);
        _context.SaveChanges();

        return CreatedAtAction(nameof(GetList), new { id = newList.ListID }, newList.ToSummaryDto(CurrentUserId, 0));
    }

    /// <summary>PUT api/v1/lists/{id} — update a list owned by the current user.</summary>
    [HttpPut("{id:int}")]
    public IActionResult UpdateList(int id, [FromBody] ListRequest request)
    {
        var list = _context.Lists.Include(l => l.Items).FirstOrDefault(l => l.ListID == id && l.OwnerID == CurrentUserId);
        if (list == null)
            return NotFound();

        list.ListName = request.ListName?.Trim();
        list.CircleID = request.CircleId;
        list.ListType = request.ListType;
        list.StrictStoreLocations = request.StrictStoreLocations;
        list.LastUpdatedDate = DateTime.Now;
        _context.SaveChanges();

        return Ok(list.ToSummaryDto(CurrentUserId, list.Items.Count(i => !i.IsDeleted)));
    }

    /// <summary>DELETE api/v1/lists/{id} — delete a list owned by the current user.</summary>
    [HttpDelete("{id:int}")]
    public IActionResult DeleteList(int id)
    {
        var list = _context.Lists.FirstOrDefault(l => l.ListID == id && l.OwnerID == CurrentUserId);
        if (list == null)
            return NotFound();

        _context.Lists.Remove(list);
        _context.SaveChanges();
        return NoContent();
    }

    /// <summary>POST api/v1/lists/{id}/share — create a share link for a list owned by the current user.</summary>
    [HttpPost("{id:int}/share")]
    public IActionResult Share(int id, [FromBody] ShareRequest request)
    {
        if (!UserOwnsList(id))
            return NotFound();

        var share = new ListShare
        {
            ListID = id,
            OwnerID = CurrentUserId,
            Permission = request.Permission,
            Token = Guid.NewGuid().ToString("N"),
            CreatedDate = DateTime.Now
        };

        _context.ListShares.Add(share);
        _context.SaveChanges();

        var shareLink = Url.Action("Manage", "Lists", new { id, shareToken = share.Token }, Request.Scheme);

        return Ok(new ShareResultDto(share.ToDto(), shareLink));
    }
}
