using ListManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ListManager.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<List> Lists => Set<List>();
    public DbSet<ListItem> ListItems => Set<ListItem>();
    public DbSet<ListShare> ListShares => Set<ListShare>();
    public DbSet<StoreLocationOption> StoreLocationOptions => Set<StoreLocationOption>();
    public DbSet<ItemLocationPreference> ItemLocationPreferences => Set<ItemLocationPreference>();
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
            .HasMany(list => list.StoreLocationOptions)
            .WithOne(option => option.List!)
            .HasForeignKey(option => option.ListID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<List>()
            .HasMany(list => list.ItemLocationPreferences)
            .WithOne(preference => preference.List!)
            .HasForeignKey(preference => preference.ListID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<List>()
            .HasMany(list => list.Shares)
            .WithOne(share => share.List!)
            .HasForeignKey(share => share.ListID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<List>()
            .HasOne(list => list.Circle)
            .WithMany()
            .HasForeignKey(list => list.CircleID)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ListShare>()
            .HasIndex(share => share.Token)
            .IsUnique();

        modelBuilder.Entity<ListItem>()
            .HasOne(item => item.StoreLocationOption)
            .WithMany(option => option.ListItems)
            .HasForeignKey(item => item.StoreLocationOptionID)
            .OnDelete(DeleteBehavior.ClientSetNull);

        modelBuilder.Entity<StoreLocationOption>()
            .HasIndex(option => new { option.ListID, option.NormalizedName })
            .IsUnique();

        modelBuilder.Entity<ItemLocationPreference>()
            .HasIndex(preference => new { preference.ListID, preference.ItemNameNormalized })
            .IsUnique();

        modelBuilder.Entity<ItemLocationPreference>()
            .HasOne(preference => preference.StoreLocationOption)
            .WithMany()
            .HasForeignKey(preference => preference.StoreLocationOptionID)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Circle>()
            .HasMany(c => c.Members)
            .WithOne(m => m.Circle)
            .HasForeignKey(m => m.CircleID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Circle>()
            .HasMany(c => c.Invitations)
            .WithOne(i => i.Circle)
            .HasForeignKey(i => i.CircleID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CircleMember>()
            .HasIndex(m => new { m.CircleID, m.UserID })
            .IsUnique();

        modelBuilder.Entity<CircleInvitation>()
            .HasIndex(i => i.Token)
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}
