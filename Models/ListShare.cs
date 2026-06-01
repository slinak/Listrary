using System.ComponentModel.DataAnnotations;

namespace ListManager.Models;

public enum ListSharePermission
{
    View,
    Edit
}

public class ListShare
{
    [Key]
    public int ListShareId { get; set; }

    public int ListID { get; set; }
    public List? List { get; set; }

    public string Token { get; set; } = Guid.NewGuid().ToString("N");
    public ListSharePermission Permission { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string? RecipientId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public bool IsAssigned => !string.IsNullOrEmpty(RecipientId);
}
