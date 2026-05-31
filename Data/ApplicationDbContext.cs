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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<List>()
            .HasMany(list => list.Items)
            .WithOne(item => item.List!)
            .HasForeignKey(item => item.ListID)
            .OnDelete(DeleteBehavior.Cascade);

        base.OnModelCreating(modelBuilder);
    }
}
