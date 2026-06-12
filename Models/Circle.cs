using System.ComponentModel.DataAnnotations;

namespace ListManager.Models;

public class Circle
{
    [Key]
    public int CircleId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    // Owner of the circle
    public string OwnerId { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public ICollection<CircleMember> Members { get; set; } = new List<CircleMember>();
    public ICollection<CircleInvitation> Invitations { get; set; } = new List<CircleInvitation>();
}

public class CircleMember
{
    [Key]
    public int CircleMemberId { get; set; }

    public int CircleId { get; set; }
    public Circle? Circle { get; set; }

    // Identity user id
    public string UserId { get; set; } = string.Empty;

    public DateTime JoinedDate { get; set; } = DateTime.Now;
}

public class CircleInvitation
{
    [Key]
    public int CircleInvitationId { get; set; }

    public int CircleId { get; set; }
    public Circle? Circle { get; set; }

    // Who sent the invite
    public string InviterId { get; set; } = string.Empty;

    // Recipient can be an email (for external invites) or a user id if known
    public string? RecipientEmail { get; set; }
    public string? RecipientId { get; set; }

    public string Token { get; set; } = Guid.NewGuid().ToString("N");

    public bool Accepted { get; set; } = false;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
