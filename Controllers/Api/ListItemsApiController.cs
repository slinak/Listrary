using System.Security.Claims;
using ListManager.Data;
using ListManager.Dtos;
using ListManager.Models;
using ListManager.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ListManager.Controllers.Api;

/// <summary>
/// JSON REST API for ListItems, consumed by the Android client. Delegates all
/// store-location/suggestion business logic to <see cref="IListItemService"/>, the
/// same service used by the MVC <see cref="ListItemController"/>.
/// </summary>
[ApiController]
[Route("api/v1/lists/{listId:int}/items")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ListItemsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IListItemService _listItemService;

    public ListItemsApiController(ApplicationDbContext context, IListItemService listItemService)
    {
        _context = context;
        _listItemService = listItemService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private bool UserOwnsList(int listId) => _context.Lists.Any(l => l.ListID == listId && l.OwnerID == CurrentUserId);

    private ListShare? FindShare(int listId, string? shareToken)
    {
        if (!string.IsNullOrWhiteSpace(shareToken))
        {
            var share = _context.ListShares.FirstOrDefault(s => s.ListID == listId && s.Token == shareToken);
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

        return _context.ListShares.FirstOrDefault(s => s.ListID == listId && s.RecipientID == CurrentUserId);
    }

    private bool HasEditAccess(int listId, string? shareToken)
    {
        if (UserOwnsList(listId))
            return true;

        var share = FindShare(listId, shareToken);
        return share != null && share.Permission == ListSharePermission.Edit;
    }

    /// <summary>GET items for a list (ordered like the web "manage" view).</summary>
    [HttpGet]
    public IActionResult GetItems(int listId, [FromQuery] string? shareToken)
    {
        if (!HasEditAccess(listId, shareToken))
        {
            // View-only share tokens can still read items.
            var viewShare = FindShare(listId, shareToken);
            if (viewShare == null && !UserOwnsList(listId))
                return NotFound();
        }

        var items = _context.ListItems.Where(li => li.ListID == listId && !li.IsDeleted);
        var ordered = _listItemService.GetManageViewOrderedItems(items);
        return Ok(ordered.Select(i => i.ToDto()).ToList());
    }

    /// <summary>POST api/v1/lists/{listId}/items — add an item.</summary>
    [HttpPost]
    public IActionResult CreateItem(int listId, [FromBody] ListItemRequest request, [FromQuery] string? shareToken)
    {
        if (!HasEditAccess(listId, shareToken))
            return NotFound();

        var list = _context.Lists.FirstOrDefault(l => l.ListID == listId);
        if (list == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.ItemName))
            return BadRequest(new { error = "ItemName is required." });

        var shouldUseStoreLocation = list.ListType == ListType.Shopping;
        var rawStoreLocation = shouldUseStoreLocation ? request.StoreLocation : null;

        var locationResolution = _listItemService.ResolveStoreLocationOption(listId, rawStoreLocation, list.StrictStoreLocations);
        if (!string.IsNullOrWhiteSpace(locationResolution.error))
            return BadRequest(new { error = locationResolution.error });

        var item = new ListItem
        {
            ListID = listId,
            ItemName = request.ItemName.Trim(),
            ItemDetails = request.ItemDetails,
            StoreLocationOption = locationResolution.option,
            StoreLocation = locationResolution.option?.Name,
            CreatedDate = DateTime.Now,
            LastUpdatedDate = DateTime.Now,
            AddedDate = DateTime.Now
        };

        _context.ListItems.Add(item);
        _context.SaveChanges();

        if (locationResolution.option?.StoreLocationOptionID > 0)
        {
            _listItemService.LearnItemLocationPreference(listId, item.ItemName, locationResolution.option.StoreLocationOptionID);
            _context.SaveChanges();
        }

        return CreatedAtAction(nameof(GetItems), new { listId }, new ListItemResultDto(item.ToDto(), locationResolution.warning));
    }

    /// <summary>PUT api/v1/lists/{listId}/items/{itemId} — update an item.</summary>
    [HttpPut("{itemId:int}")]
    public IActionResult UpdateItem(int listId, int itemId, [FromBody] ListItemRequest request, [FromQuery] string? shareToken)
    {
        if (!HasEditAccess(listId, shareToken))
            return NotFound();

        var list = _context.Lists.FirstOrDefault(l => l.ListID == listId);
        if (list == null)
            return NotFound();

        var item = _context.ListItems.FirstOrDefault(li => li.ListID == listId && li.ListItemID == itemId);
        if (item == null)
            return NotFound();

        var shouldUseStoreLocation = list.ListType == ListType.Shopping;
        var rawStoreLocation = shouldUseStoreLocation ? request.StoreLocation : null;

        var locationResolution = _listItemService.ResolveStoreLocationOption(listId, rawStoreLocation, list.StrictStoreLocations);
        if (!string.IsNullOrWhiteSpace(locationResolution.error))
            return BadRequest(new { error = locationResolution.error });

        item.ItemName = request.ItemName;
        item.ItemDetails = request.ItemDetails;
        item.StoreLocationOption = locationResolution.option;
        item.StoreLocation = locationResolution.option?.Name;
        item.IsCompleted = request.IsCompleted;
        item.LastUpdatedDate = DateTime.Now;
        _context.SaveChanges();

        if (locationResolution.option?.StoreLocationOptionID > 0)
        {
            _listItemService.LearnItemLocationPreference(listId, item.ItemName, locationResolution.option.StoreLocationOptionID);
            _context.SaveChanges();
        }

        return Ok(new ListItemResultDto(item.ToDto(), locationResolution.warning));
    }

    /// <summary>DELETE api/v1/lists/{listId}/items/{itemId}.</summary>
    [HttpDelete("{itemId:int}")]
    public IActionResult DeleteItem(int listId, int itemId, [FromQuery] string? shareToken)
    {
        if (!HasEditAccess(listId, shareToken))
            return NotFound();

        var item = _context.ListItems.FirstOrDefault(li => li.ListID == listId && li.ListItemID == itemId);
        if (item == null)
            return NotFound();

        item.IsDeleted = true;
        _context.ListItems.Remove(item);
        _context.SaveChanges();
        return NoContent();
    }

    /// <summary>POST api/v1/lists/{listId}/items/{itemId}/toggle — toggle completion.</summary>
    [HttpPost("{itemId:int}/toggle")]
    public IActionResult ToggleItem(int listId, int itemId, [FromQuery] string? shareToken)
    {
        if (!HasEditAccess(listId, shareToken))
            return NotFound();

        var item = _context.ListItems.FirstOrDefault(li => li.ListID == listId && li.ListItemID == itemId);
        if (item == null)
            return NotFound();

        item.IsCompleted = !item.IsCompleted;
        item.LastUpdatedDate = DateTime.Now;

        if (item.IsCompleted)
        {
            var maxSortOrder = _context.ListItems
                .Where(li => li.ListID == listId && !li.IsDeleted)
                .Max(li => (int?)li.SortOrder) ?? 0;
            item.SortOrder = maxSortOrder + 1;
        }

        _context.SaveChanges();
        return Ok(item.ToDto());
    }

    /// <summary>POST api/v1/lists/{listId}/items/mark-all-incomplete.</summary>
    [HttpPost("mark-all-incomplete")]
    public IActionResult MarkAllIncomplete(int listId, [FromQuery] string? shareToken)
    {
        if (!HasEditAccess(listId, shareToken))
            return NotFound();

        var completedItems = _context.ListItems
            .Where(li => li.ListID == listId && !li.IsDeleted && li.IsCompleted)
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

        return NoContent();
    }

    /// <summary>GET api/v1/lists/{listId}/items/suggestions/names?query=.</summary>
    [HttpGet("suggestions/names")]
    public IActionResult GetItemNameSuggestions(int listId, [FromQuery] string? query, [FromQuery] string? shareToken)
    {
        if (!HasEditAccess(listId, shareToken))
            return Unauthorized();

        return Ok(_listItemService.GetItemNameSuggestions(listId, query));
    }

    /// <summary>GET api/v1/lists/{listId}/items/suggestions/locations?itemName=&amp;query=.</summary>
    [HttpGet("suggestions/locations")]
    public IActionResult GetStoreLocationSuggestions(int listId, [FromQuery] string? itemName, [FromQuery] string? query, [FromQuery] string? shareToken)
    {
        if (!HasEditAccess(listId, shareToken))
            return Unauthorized();

        return Ok(_listItemService.GetStoreLocationSuggestions(listId, itemName, query));
    }
}
