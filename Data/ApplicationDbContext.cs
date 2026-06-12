using ListManager.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ListManager.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<List> Lists => Set<List>();
    public DbSet<ListItem> ListItems => Set<ListItem>();
    public DbSet<ListShare> ListShares => Set<ListShare>();
    public DbSet<Circle> Circles => Set<Circle>();
    public DbSet<CircleMember> CircleMembers => Set<CircleMember>();
    public DbSet<CircleInvitation> CircleInvitations => Set<CircleInvitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<List>()
            .HasMany(list => list.Items)
            .WithOne(item => item.List!)
            .HasForeignKey(item => item.ListID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<List>()
            .HasMany(list => list.Shares)
            .WithOne(share => share.List!)
            .HasForeignKey(share => share.ListID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<List>()
            .HasOne(list => list.Circle)
            .WithMany()
            .HasForeignKey(list => list.CircleId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ListShare>()
            .HasIndex(share => share.Token)
            .IsUnique();

        modelBuilder.Entity<Circle>()
            .HasMany(c => c.Members)
            .WithOne(m => m.Circle)
            .HasForeignKey(m => m.CircleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Circle>()
            .HasMany(c => c.Invitations)
            .WithOne(i => i.Circle)
            .HasForeignKey(i => i.CircleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CircleMember>()
            .HasIndex(m => new { m.CircleId, m.UserId })
            .IsUnique();

        modelBuilder.Entity<CircleInvitation>()
            .HasIndex(i => i.Token)
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}
