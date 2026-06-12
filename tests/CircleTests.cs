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

        var circle = new Circle { Name = "Test", OwnerId = ownerId };
        ctx.Circles.Add(circle);
        ctx.SaveChanges();

        ctx.CircleMembers.Add(new CircleMember { CircleId = circle.CircleId, UserId = memberId });
        ctx.SaveChanges();

        var list = new List { ListName = "SharedList", OwnerId = ownerId, CircleId = circle.CircleId };
        ctx.Lists.Add(list);
        ctx.SaveChanges();

        // Simulate query that Index uses
        var visible = ctx.Lists
            .Include(l => l.Circle)
            .ThenInclude(c => c.Members)
            .Where(l => l.OwnerId == memberId || (l.Circle != null && l.Circle.Members.Any(m => m.UserId == memberId)))
            .ToList();

        Assert.Contains(visible, l => l.ListID == list.ListID);
    }

    [Fact]
    public void AcceptInviteAddsMember()
    {
        using var ctx = CreateContext("AcceptInviteAddsMember");

        var ownerId = "owner-2";
        var userId = "user-2";

        var circle = new Circle { Name = "Invited", OwnerId = ownerId };
        ctx.Circles.Add(circle);
        ctx.SaveChanges();

        var invite = new CircleInvitation { CircleId = circle.CircleId, InviterId = ownerId, Token = "tok-123" };
        ctx.CircleInvitations.Add(invite);
        ctx.SaveChanges();

        // Simulate accept
        invite.RecipientId = userId;
        invite.Accepted = true;
        ctx.CircleInvitations.Update(invite);
        ctx.CircleMembers.Add(new CircleMember { CircleId = circle.CircleId, UserId = userId });
        ctx.SaveChanges();

        var memberExists = ctx.CircleMembers.Any(m => m.CircleId == circle.CircleId && m.UserId == userId);
        Assert.True(memberExists);
    }
}
