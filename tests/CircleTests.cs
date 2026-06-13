using System.Linq;
using ListManager.Data;
using ListManager.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ListManager.Tests;

public class CircleTests
{
    private ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void MemberCanViewCircleList()
    {
        using var ctx = CreateContext("MemberCanViewCircleList");

        var ownerId = "owner-1";
        var memberId = "member-1";

        var circle = new Circle { Name = "Test", OwnerID = ownerId };
        ctx.Circles.Add(circle);
        ctx.SaveChanges();

        ctx.CircleMembers.Add(new CircleMember { CircleID = circle.CircleID, UserID = memberId });
        ctx.SaveChanges();

        var list = new List { ListName = "SharedList", OwnerID = ownerId, CircleID = circle.CircleID };
        ctx.Lists.Add(list);
        ctx.SaveChanges();

        // Simulate query that Index uses
        var visible = ctx.Lists
            .Include(l => l.Circle)
            .ThenInclude(c => c.Members)
            .Where(l => l.OwnerID == memberId || (l.Circle != null && l.Circle.Members.Any(m => m.UserID == memberId)))
            .ToList();

        Assert.Contains(visible, l => l.ListID == list.ListID);
    }

    [Fact]
    public void AcceptInviteAddsMember()
    {
        using var ctx = CreateContext("AcceptInviteAddsMember");

        var ownerId = "owner-2";
        var userId = "user-2";

        var circle = new Circle { Name = "Invited", OwnerID = ownerId };
        ctx.Circles.Add(circle);
        ctx.SaveChanges();

        var invite = new CircleInvitation { CircleID = circle.CircleID, InviterID = ownerId, Token = "tok-123" };
        ctx.CircleInvitations.Add(invite);
        ctx.SaveChanges();

        // Simulate accept
        invite.RecipientID = userId;
        invite.Accepted = true;
        ctx.CircleInvitations.Update(invite);
        ctx.CircleMembers.Add(new CircleMember { CircleId = circle.CircleID, UserID = userId });
        ctx.SaveChanges();

        var memberExists = ctx.CircleMembers.Any(m => m.CircleID == circle.CircleID && m.UserID == userId);
        Assert.True(memberExists);
    }
}
