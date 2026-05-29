namespace ListManager.Models;

public class ListItemManagementViewModel
{
    public List ListModel { get; set; } = new List();
    public List<ListItem> Items { get; set; } = new List<ListItem>();
    public ListItem NewItem { get; set; } = new ListItem();
}
