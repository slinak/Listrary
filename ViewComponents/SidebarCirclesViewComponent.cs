using System.Security.Claims;
using ListManager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListManager.ViewComponents;

// The signed-in user's circles, shown as shortcuts in the sidebar.
public class SidebarCirclesViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public SidebarCirclesViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var circles = await _context.Circles
            .Where(c => c.OwnerID == userId || c.Members.Any(m => m.UserID == userId))
            .OrderBy(c => c.Name)
            .Select(c => new SidebarCircle(c.CircleID, c.Name))
            .ToListAsync();

        return View(circles);
    }
}

public record SidebarCircle(int Id, string Name);
