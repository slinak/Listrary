namespace ListManager.Models;

public class List
{
    public string ListID { get; set; } = string.Empty;
    public string? ListName { get; set; }
    public ListType? ListType { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastUpdatedDate { get; set; }
}

public enum ListType
{
    Grocery,
    ToDo,
    WishList,
    Other
}