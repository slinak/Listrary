namespace ListManager.Models;

public class ListItem
{
    public string ListItemID { get; set; } = string.Empty;
    public string ListID { get; set; } = string.Empty;
    public string? ItemName { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public DateTime AddedDate { get; set; }
}