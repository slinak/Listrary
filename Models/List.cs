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

    public ListType? ListType { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime LastUpdatedDate { get; set; } = DateTime.Now;

    public ICollection<ListItem> Items { get; set; } = new System.Collections.Generic.List<ListItem>();
}

public enum ListType
{
    Grocery,
    ToDo,
    WishList,
    Other
}