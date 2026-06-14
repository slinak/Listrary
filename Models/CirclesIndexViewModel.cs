using System.Collections.Generic;

namespace ListManager.Models;

public class CirclesIndexViewModel
{
    public IEnumerable<Circle> Circles { get; set; } = new List<Circle>();
    public IEnumerable<CircleInvitation> PendingInvitations { get; set; } = new List<CircleInvitation>();
}
