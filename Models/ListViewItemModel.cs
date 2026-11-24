using ListManager.Services;

namespace ListManager.Models;

public class ListViewItemModel
{
    public const int PreviewSize = 3;

    public List? ListModel { get; set; }
    public int ItemCount { get; set; }
    public int CompletedCount { get; set; }

    // The next few unchecked items, in the same order as the list page, shown on the list card.
    public IEnumerable<string> UpcomingItems { get; set; } = new List<string>();

    public int RemainingCount => ItemCount - CompletedCount;
    public int Percent => ItemCount == 0 ? 0 : (int)Math.Round(100.0 * CompletedCount / ItemCount);

    // Expects list.Items to be loaded.
    public static ListViewItemModel FromList(List list)
    {
        var items = list.Items.Where(i => !i.IsDeleted).ToList();
        return new ListViewItemModel
        {
            ListModel = list,
            ItemCount = items.Count,
            CompletedCount = items.Count(i => i.IsCompleted),
            UpcomingItems = ListItemService.OrderForManageView(items.Where(i => !i.IsCompleted))
                .Take(PreviewSize)
                .Select(i => i.ItemName ?? string.Empty)
                .ToList()
        };
    }
}
