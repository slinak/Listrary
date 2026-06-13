using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ListManager.Models;

public class List
{
    [Key]
    public int ListID { get; set; }

    [Required]
    [StringLength(100)]
    public string? ListName { get; set; }
    public string? OwnerID { get; set; }
    public int? CircleID { get; set; }
    public Circle? Circle { get; set; }
    public ListType? ListType { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime LastUpdatedDate { get; set; } = DateTime.Now;
    public bool IsDeleted { get; set; } = false;
    public bool IsCompleted { get; set; } = false;

    public ICollection<ListItem> Items { get; set; } = new System.Collections.Generic.List<ListItem>();
    public ICollection<ListShare> Shares { get; set; } = new System.Collections.Generic.List<ListShare>();
}

public enum ListType
{
    Grocery,
    ToDo,
    WishList,
    Other
}