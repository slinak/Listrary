using System.Collections.Generic;

namespace ListManager.Models;

public class CirclesIndexViewModel
{
    public IEnumerable<CircleSummaryViewModel> Circles { get; set; } = new List<CircleSummaryViewModel>();
    public IEnumerable<CircleInvitation> PendingInvitations { get; set; } = new List<CircleInvitation>();
}

// A circle together with its lists, used by the Circles index cards and the circle detail page.
public class CircleSummaryViewModel
{
    public Circle Circle { get; set; } = null!;
    public IEnumerable<ListViewItemModel> Lists { get; set; } = new List<ListViewItemModel>();
    public bool IsOwner { get; set; }

    public DateTime LastActivity => Lists
        .Select(l => l.ListModel?.LastUpdatedDate ?? Circle.CreatedDate)
        .DefaultIfEmpty(Circle.CreatedDate)
        .Max();
}
