using System.Security.Claims;
using ListManager.Data;
using ListManager.Dtos;
using ListManager.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListManager.Controllers.Api;

/// <summary>
/// JSON REST API for Circles, consumed by the Android client. Mirrors the behavior of
/// <see cref="CirclesController"/> but returns DTOs and authenticates via Entra ID JWT
/// bearer tokens.
/// </summary>
[ApiController]
[Route("api/v1/circles")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CirclesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CirclesApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string? CurrentUserEmail => User.FindFirstValue(ClaimTypes.Email);

    /// <summary>GET api/v1/circles — circles the user owns/belongs to, plus pending invitations.</summary>
    [HttpGet]
    public IActionResult GetCircles()
    {
        var userId = CurrentUserId;
        var userEmail = CurrentUserEmail?.Trim();

        var circles = _context.Circles
            .Include(c => c.Members)
            .Include(c => c.Invitations)
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

        return Ok(new CirclesIndexDto(
            circles.Select(c => c.ToDto()).ToList(),
            pendingInvites.Select(i => i.ToDto()).ToList()));
    }

    /// <summary>GET api/v1/circles/{id} — circle detail (members + invitations), "manage" view.</summary>
    [HttpGet("{id:int}")]
    public IActionResult GetCircle(int id)
    {
        var circle = _context.Circles
            .Include(c => c.Members)
            .Include(c => c.Invitations)
            .FirstOrDefault(c => c.CircleID == id);

        if (circle == null)
            return NotFound();

        if (!CirclesHelper.isCircleMember(circle, CurrentUserId))
            return Forbid();

        return Ok(circle.ToDto());
    }

    /// <summary>POST api/v1/circles — create a circle (current user becomes owner + member).</summary>
    [HttpPost]
    public IActionResult CreateCircle([FromBody] CreateCircleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });

        var circle = new Circle
        {
            Name = request.Name.Trim(),
            OwnerID = CurrentUserId,
            CreatedDate = DateTime.Now
        };

        _context.Circles.Add(circle);
        _context.SaveChanges();

        _context.CircleMembers.Add(new CircleMember { CircleID = circle.CircleID, UserID = CurrentUserId, JoinedDate = DateTime.Now });
        _context.SaveChanges();

        return CreatedAtAction(nameof(GetCircle), new { id = circle.CircleID }, circle.ToDto());
    }

    /// <summary>POST api/v1/circles/{id}/invite — invite a member by email or user id.</summary>
    [HttpPost("{id:int}/invite")]
    public IActionResult Invite(int id, [FromBody] InviteRequest request)
    {
        var circle = _context.Circles.Include(c => c.Members).FirstOrDefault(c => c.CircleID == id);
        if (circle == null)
            return NotFound();

        if (!CirclesHelper.isCircleMember(circle, CurrentUserId))
            return Forbid();

        var invite = new CircleInvitation
        {
            CircleID = id,
            InviterID = CurrentUserId,
            RecipientEmail = string.IsNullOrWhiteSpace(request.RecipientEmail) ? null : request.RecipientEmail.Trim(),
            RecipientID = string.IsNullOrWhiteSpace(request.RecipientId) ? null : request.RecipientId.Trim(),
            Token = Guid.NewGuid().ToString("N"),
            CreatedDate = DateTime.Now
        };

        _context.CircleInvitations.Add(invite);
        _context.SaveChanges();

        invite.Circle = circle;
        return Ok(invite.ToDto());
    }

    /// <summary>DELETE api/v1/circles/{id}/members/{userId} — remove a member (owner only).</summary>
    [HttpDelete("{id:int}/members/{userId}")]
    public IActionResult RemoveMember(int id, string userId)
    {
        var circle = _context.Circles.Include(c => c.Members).FirstOrDefault(c => c.CircleID == id);
        if (circle == null)
            return NotFound();

        if (!CirclesHelper.isCircleOwner(circle, CurrentUserId))
            return Forbid();

        var member = _context.CircleMembers.FirstOrDefault(m => m.CircleID == id && m.UserID == userId);
        if (member == null)
            return NotFound();

        _context.CircleMembers.Remove(member);
        _context.SaveChanges();
        return NoContent();
    }

    /// <summary>POST api/v1/circles/{id}/leave — current user leaves a circle.</summary>
    [HttpPost("{id:int}/leave")]
    public IActionResult Leave(int id)
    {
        var member = _context.CircleMembers.FirstOrDefault(m => m.CircleID == id && m.UserID == CurrentUserId);
        if (member != null)
        {
            _context.CircleMembers.Remove(member);
            _context.SaveChanges();
        }

        return NoContent();
    }

    /// <summary>POST api/v1/circles/invitations/{invitationId}/accept.</summary>
    [HttpPost("invitations/{invitationId:int}/accept")]
    public IActionResult AcceptInvite(int invitationId)
    {
        var invite = _context.CircleInvitations.Include(i => i.Circle).FirstOrDefault(i => i.CircleInvitationID == invitationId && !i.Accepted);
        if (invite == null)
            return NotFound();

        var userId = CurrentUserId;
        if (!CirclesHelper.isCorrectRecipient(invite, userId, CurrentUserEmail?.Trim()))
            return Forbid();

        if (invite.RecipientID == null)
            invite.RecipientID = userId;

        invite.Accepted = true;

        var exists = _context.CircleMembers.Any(m => m.CircleID == invite.CircleID && m.UserID == userId);
        if (!exists)
            _context.CircleMembers.Add(new CircleMember { CircleID = invite.CircleID, UserID = userId, JoinedDate = DateTime.Now });

        _context.SaveChanges();
        return NoContent();
    }

    /// <summary>POST api/v1/circles/invitations/{invitationId}/reject.</summary>
    [HttpPost("invitations/{invitationId:int}/reject")]
    public IActionResult RejectInvite(int invitationId)
    {
        var invite = _context.CircleInvitations.Include(i => i.Circle).FirstOrDefault(i => i.CircleInvitationID == invitationId && !i.Accepted);
        if (invite == null)
            return NotFound();

        if (!CirclesHelper.isCorrectRecipient(invite, CurrentUserId, CurrentUserEmail?.Trim()))
            return Forbid();

        _context.CircleInvitations.Remove(invite);
        _context.SaveChanges();
        return NoContent();
    }

    /// <summary>DELETE api/v1/circles/invitations/{invitationId} — cancel a pending invitation.</summary>
    [HttpDelete("invitations/{invitationId:int}")]
    public IActionResult RemoveInvitation(int invitationId)
    {
        var invite = _context.CircleInvitations.Include(i => i.Circle).FirstOrDefault(i => i.CircleInvitationID == invitationId);
        if (invite == null)
            return NotFound();

        var isOwnerOrInviter = invite.Circle?.OwnerID == CurrentUserId || invite.InviterID == CurrentUserId;
        if (!isOwnerOrInviter)
            return Forbid();

        _context.CircleInvitations.Remove(invite);
        _context.SaveChanges();
        return NoContent();
    }

    /// <summary>DELETE api/v1/circles/{id} — delete a circle (owner only).</summary>
    [HttpDelete("{id:int}")]
    public IActionResult DeleteCircle(int id)
    {
        var circle = _context.Circles.FirstOrDefault(c => c.CircleID == id);
        if (circle == null)
            return NotFound();

        if (circle.OwnerID != CurrentUserId)
            return Forbid();

        _context.Circles.Remove(circle);
        _context.SaveChanges();
        return NoContent();
    }
}
