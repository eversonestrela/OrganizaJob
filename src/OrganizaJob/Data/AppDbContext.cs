using Microsoft.EntityFrameworkCore;
using OrganizaJob.Models;

namespace OrganizaJob.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Item> Itens => Set<Item>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        var item = mb.Entity<Item>();
        item.Property(i => i.Tipo).HasConversion<string>();
        item.Property(i => i.Repetir).HasConversion<string>();
        item.HasIndex(i => i.Tipo);
    }
}
