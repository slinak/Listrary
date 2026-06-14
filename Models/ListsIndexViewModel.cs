namespace ListManager.Models;

public class ListsIndexViewModel
{
    public IEnumerable<CircleListsViewModel> CircleGroups { get; set; } = new List<CircleListsViewModel>();
    public IEnumerable<ListViewItemModel> OtherLists { get; set; } = new List<ListViewItemModel>();
}

public class CircleListsViewModel
{
    public Circle Circle { get; set; } = null!;
    public IEnumerable<ListViewItemModel> Lists { get; set; } = new List<ListViewItemModel>();
}
