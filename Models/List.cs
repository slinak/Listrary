using System.ComponentModel.DataAnnotations;

namespace ListManager.Models;

public class List
{
    [Key]
    public int ListID { get; set; }
    public string? ListName { get; set; }
    public ListType? ListType { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime LastUpdatedDate { get; set; } = DateTime.Now;
}

public enum ListType
{
    Grocery,
    ToDo,
    WishList,
    Other
}