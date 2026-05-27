using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using SafeSight.Api.Models.Domain;

namespace SafeSight.Api.Data.Mongo;

public class SafeSightDbContext : DbContext
{
    public SafeSightDbContext(DbContextOptions<SafeSightDbContext> options) : base(options) { }

    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<HeatmapCell> HeatmapCells => Set<HeatmapCell>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Alert>().ToCollection("alerts");
        modelBuilder.Entity<HeatmapCell>().ToCollection("heatmap_cells");

        modelBuilder.Entity<Alert>().OwnsOne(a => a.MissingPerson);
        modelBuilder.Entity<Alert>().OwnsOne(a => a.LastKnownLocation);
    }
}
