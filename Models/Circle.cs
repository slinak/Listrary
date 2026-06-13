using System.ComponentModel.DataAnnotations;

namespace ListManager.Models;

public class Circle
{
    [Key]
    public int CircleID { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    public string OwnerID { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public ICollection<CircleMember> Members { get; set; } = new List<CircleMember>();
    public ICollection<CircleInvitation> Invitations { get; set; } = new List<CircleInvitation>();
}

public class CircleMember
{
    [Key]
    public int CircleMemberID { get; set; }

    public int CircleID { get; set; }
    public Circle? Circle { get; set; }
    public string UserID { get; set; } = string.Empty;

    public DateTime JoinedDate { get; set; } = DateTime.Now;
}

public class CircleInvitation
{
    [Key]
    public int CircleInvitationID { get; set; }

    public int CircleID { get; set; }
    public Circle? Circle { get; set; }

    // Who sent the invite
    public string InviterID { get; set; } = string.Empty;

    // Recipient can be an email (for external invites) or a user id if known
    public string? RecipientEmail { get; set; }
    public string? RecipientID { get; set; }

    public string Token { get; set; } = Guid.NewGuid().ToString("N");

    public bool Accepted { get; set; } = false;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
