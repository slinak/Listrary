namespace ListManager.Models;

public class ListItemManagementViewModel
{
    public List ListModel { get; set; } = new List();
    public List<ListItem> Items { get; set; } = new List<ListItem>();
    public ListItem NewItem { get; set; } = new ListItem();
    public bool CanEdit { get; set; } = true;
    public string? ShareToken { get; set; }
    public string? GeneratedShareLink { get; set; }
    public List<ListShare> ActiveShares { get; set; } = new List<ListShare>();
}
