using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using SafeSight.Api.Models.Domain;

namespace SafeSight.Api.Data.Mongo;

public class SafeSightDbContext : DbContext
{
    public SafeSightDbContext(DbContextOptions<SafeSightDbContext> options) : base(options)
    {
        // MongoDB standalone (sin replica set) no soporta transacciones
        Database.AutoTransactionBehavior = Microsoft.EntityFrameworkCore.AutoTransactionBehavior.Never;
    }

    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<HeatmapCell> HeatmapCells => Set<HeatmapCell>();
    public DbSet<InfoReportDocument> InfoReports => Set<InfoReportDocument>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
    public DbSet<AdminActivity> AdminActivities => Set<AdminActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Alert>().ToCollection("alerts");
        modelBuilder.Entity<HeatmapCell>().ToCollection("heatmap_cells");
        modelBuilder.Entity<InfoReportDocument>().ToCollection("info_reports");
        modelBuilder.Entity<AdminActivity>().ToCollection("admin_activity");

        modelBuilder.Entity<Alert>().OwnsOne(a => a.MissingPerson);
        modelBuilder.Entity<Alert>().OwnsOne(a => a.LastKnownLocation);

        modelBuilder.Entity<DeviceToken>().ToCollection("device_tokens");
    }
}
