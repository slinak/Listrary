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

        modelBuilder.Entity<ListShare>()
            .HasIndex(share => share.Token)
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}
