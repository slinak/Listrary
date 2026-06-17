using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace ListManager.Models;

public class List
{
    [Key]
    public int ListID { get; set; }

    [Required]
    [StringLength(100)]
    [Description("Name")]
    public string? ListName { get; set; }
    public string? OwnerID { get; set; }
    public int? CircleID { get; set; }
    public Circle? Circle { get; set; }

    [Description("Type")]
    public ListType? ListType { get; set; }

    [Description("Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Description("Last Updated Date")]
    public DateTime LastUpdatedDate { get; set; } = DateTime.Now;

    public bool IsDeleted { get; set; } = false;
    public bool IsCompleted { get; set; } = false;

    public ICollection<ListItem> Items { get; set; } = new System.Collections.Generic.List<ListItem>();
    public ICollection<ListShare> Shares { get; set; } = new System.Collections.Generic.List<ListShare>();
}

public enum ListType
{
    Grocery,
    [Description("To Do")]
    ToDo,
    [Description("Wish List")]
    WishList,
    [Description("Other")]
    Other
}