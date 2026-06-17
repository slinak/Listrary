using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ListManager.Data;
using ListManager.Models;

namespace ListManager.Controllers;

[Authorize]
public class CirclesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CirclesController> _logger;

    public CirclesController(ApplicationDbContext context, ILogger<CirclesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string? CurrentUserEmail => User.FindFirstValue(ClaimTypes.Email);

    public IActionResult Index()
    {
        var userId = CurrentUserId;
        var userEmail = CurrentUserEmail?.Trim();

        var circles = _context.Circles
            .Include(c => c.Members)
            .Where(c => c.OwnerID == userId || c.Members.Any(m => m.UserID == userId))
            .OrderBy(c => c.Name)
            .ToList();

        var pendingInvites = _context.CircleInvitations
            .Include(i => i.Circle)
            .Where(i => !i.Accepted &&
                (i.RecipientID == userId ||
                 (i.RecipientID == null && userEmail != null && i.RecipientEmail != null && i.RecipientEmail.Equals(userEmail))))
            .OrderBy(i => i.CreatedDate)
            .ToList();

        var viewModel = new CirclesIndexViewModel
        {
            Circles = circles,
            PendingInvitations = pendingInvites
        };

        return View(viewModel);
    }

    [HttpPost]
    public IActionResult AcceptInvite(int invitationId)
    {
        var invite = _context.CircleInvitations
            .Include(i => i.Circle)
            .FirstOrDefault(i => i.CircleInvitationID == invitationId && !i.Accepted);

        if (invite == null)
            return NotFound();

        var userId = CurrentUserId;
        var userEmail = CurrentUserEmail?.Trim();
        var isRecipient = invite.RecipientID == userId ||
            (invite.RecipientID == null && userEmail != null && invite.RecipientEmail != null && invite.RecipientEmail.Equals(userEmail, StringComparison.OrdinalIgnoreCase));

        if (!isRecipient)
            return Forbid();

        if (invite.RecipientID == null)
            invite.RecipientID = userId;

        invite.Accepted = true;

        var exists = _context.CircleMembers.Any(m => m.CircleID == invite.CircleID && m.UserID == userId);
        if (!exists)
        {
            _context.CircleMembers.Add(new CircleMember { CircleID = invite.CircleID, UserID = userId, JoinedDate = DateTime.Now });
        }

        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult RejectInvite(int invitationId)
    {
        var invite = _context.CircleInvitations
            .Include(i => i.Circle)
            .FirstOrDefault(i => i.CircleInvitationID == invitationId && !i.Accepted);

        if (invite == null)
            return NotFound();

        var userId = CurrentUserId;
        var userEmail = CurrentUserEmail?.Trim();
        var isRecipient = invite.RecipientID == userId ||
            (invite.RecipientID == null && userEmail != null && invite.RecipientEmail != null && invite.RecipientEmail.Equals(userEmail, StringComparison.OrdinalIgnoreCase));

        if (!isRecipient)
            return Forbid();

        _context.CircleInvitations.Remove(invite);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    public IActionResult Create()
    {
        return View(new Circle());
    }

    [HttpPost]
    public IActionResult Create(Circle model)
    {
        if (!ModelState.IsValid)
            return View(model);

        model.OwnerID = CurrentUserId;
        model.CreatedDate = DateTime.Now;

        _context.Circles.Add(model);
        _context.SaveChanges();

        // Add owner as member
        var ownerMember = new CircleMember { CircleID = model.CircleID, UserID = CurrentUserId, JoinedDate = DateTime.Now };
        _context.CircleMembers.Add(ownerMember);
        _context.SaveChanges();

        return RedirectToAction("Manage", new { id = model.CircleID });
    }

    public IActionResult Manage(int id)
    {
        var circle = _context.Circles
            .Include(c => c.Members)
            .Include(c => c.Invitations)
            .FirstOrDefault(c => c.CircleID == id);

        if (circle == null)
            return NotFound();

        var isMember = circle.OwnerID == CurrentUserId || circle.Members.Any(m => m.UserID == CurrentUserId);
        if (!isMember)
            return Forbid();

        return View(circle);
    }

    [HttpPost]
    public IActionResult Invite(int circleId, string? recipientEmail, string? recipientId)
    {
        var circle = _context.Circles.Include(c => c.Members).FirstOrDefault(c => c.CircleID == circleId);
        if (circle == null)
            return NotFound();

        var isMember = circle.OwnerID == CurrentUserId || circle.Members.Any(m => m.UserID == CurrentUserId);
        if (!isMember)
            return Forbid();

        var invite = new CircleInvitation
        {
            CircleID = circleId,
            InviterID = CurrentUserId,
            RecipientEmail = string.IsNullOrWhiteSpace(recipientEmail) ? null : recipientEmail?.Trim(),
            RecipientID = string.IsNullOrWhiteSpace(recipientId) ? null : recipientId?.Trim(),
            Token = Guid.NewGuid().ToString("N"),
            CreatedDate = DateTime.Now
        };

        _context.CircleInvitations.Add(invite);
        _context.SaveChanges();

        TempData["StatusMessage"] = $"Invitation sent to {(invite.RecipientEmail ?? invite.RecipientID ?? "recipient")}.";

        return RedirectToAction("Manage", new { id = circleId });
    }

    [HttpPost]
    public IActionResult RemoveMember(int circleId, string userId)
    {
        var circle = _context.Circles.Include(c => c.Members).FirstOrDefault(c => c.CircleID == circleId);
        if (circle == null)
            return NotFound();

        var isOwner = circle.OwnerID == CurrentUserId;
        if (!isOwner)
            return Forbid();

        var member = _context.CircleMembers.FirstOrDefault(m => m.CircleID == circleId && m.UserID == userId);
        if (member != null)
        {
            _context.CircleMembers.Remove(member);
            _context.SaveChanges();
            TempData["StatusMessage"] = $"Removed member {userId}.";
        }

        return RedirectToAction("Manage", new { id = circleId });
    }

    [HttpPost]
    public IActionResult Leave(int circleId)
    {
        var userId = CurrentUserId;
        var member = _context.CircleMembers.FirstOrDefault(m => m.CircleID == circleId && m.UserID == userId);
        if (member != null)
        {
            _context.CircleMembers.Remove(member);
            _context.SaveChanges();
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult RemoveInvitation(int invitationId)
    {
        var invite = _context.CircleInvitations.Include(i => i.Circle).FirstOrDefault(i => i.CircleInvitationID == invitationId);
        if (invite == null)
            return NotFound();

        var isOwnerOrInviter = invite.Circle?.OwnerID == CurrentUserId || invite.InviterID == CurrentUserId;
        if (!isOwnerOrInviter)
            return Forbid();

        var recip = invite.RecipientEmail ?? invite.RecipientID ?? "(unspecified)";
        _context.CircleInvitations.Remove(invite);
        _context.SaveChanges();

        TempData["StatusMessage"] = $"Invitation canceled for {recip}.";

        return RedirectToAction("Manage", new { id = invite.CircleID });
    }

    [HttpPost]
    public IActionResult Delete(int circleId)
    {
        var circle = _context.Circles.FirstOrDefault(c => c.CircleID == circleId);
        if (circle == null)
            return NotFound();

        if (circle.OwnerID != CurrentUserId)
            return Forbid();

        var name = circle.Name;
        _context.Circles.Remove(circle);
        _context.SaveChanges();

        TempData["StatusMessage"] = $"Circle '{name}' deleted.";

        return RedirectToAction("Index");
    }

    [AllowAnonymous]
    public IActionResult Accept(string token)
    {
        if (string.IsNullOrEmpty(token))
            return NotFound();

        var invite = _context.CircleInvitations.Include(i => i.Circle).FirstOrDefault(i => i.Token == token);
        if (invite == null)
            return NotFound();

        if (!User.Identity?.IsAuthenticated ?? false)
        {
            // Redirect to login and then back to this action
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Accept", "Circles", new { token = token }) });
        }

        var userId = CurrentUserId;

        if (invite.RecipientID == null)
            invite.RecipientID = userId;

        invite.Accepted = true;

        // Add member if not exists
        var exists = _context.CircleMembers.Any(m => m.CircleID == invite.CircleID && m.UserID == userId);
        if (!exists)
        {
            _context.CircleMembers.Add(new CircleMember { CircleID = invite.CircleID, UserID = userId, JoinedDate = DateTime.Now });
        }

        _context.SaveChanges();

        return RedirectToAction("Manage", new { id = invite.CircleID });
    }
}
