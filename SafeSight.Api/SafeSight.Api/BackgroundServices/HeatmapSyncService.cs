using Cassandra;
using Microsoft.Extensions.Options;
using SafeSight.Api.Common;
using SafeSight.Api.Data.Cassandra;
using SafeSight.Api.Models.Configuration;
using SafeSight.Api.Models.Domain;
using SafeSight.Api.Repositories.Interfaces;

namespace SafeSight.Api.BackgroundServices;

// HeatmapSyncService implementa el patrón de consistencia eventual:
// los datos de heatmap en MongoDB están unos segundos "atrasados" respecto
// a los eventos en Cassandra. Esto es una decisión de diseño deliberada, no un defecto.
// El pipeline: Cassandra (ingesta) → sync batch → MongoDB (Data Mart analítico).
public class HeatmapSyncService : BackgroundService
{
    private const string CheckpointKey = "heatmap_sync";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CassandraSessionFactory _cassandraFactory;
    private readonly SyncSettings _syncSettings;
    private readonly HeatmapSettings _heatmapSettings;
    private readonly DatabaseSettings _dbSettings;
    private readonly ILogger<HeatmapSyncService> _logger;

    public HeatmapSyncService(
        IServiceScopeFactory scopeFactory,
        CassandraSessionFactory cassandraFactory,
        IOptions<SyncSettings> syncSettings,
        IOptions<HeatmapSettings> heatmapSettings,
        IOptions<DatabaseSettings> dbSettings,
        ILogger<HeatmapSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _cassandraFactory = cassandraFactory;
        _syncSettings = syncSettings.Value;
        _heatmapSettings = heatmapSettings.Value;
        _dbSettings = dbSettings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HeatmapSyncService iniciado. Intervalo: {Interval}s", _syncSettings.HeatmapSyncIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la sincronización del heatmap");
            }

            await Task.Delay(TimeSpan.FromSeconds(_syncSettings.HeatmapSyncIntervalSeconds), stoppingToken);
        }
    }

    private async Task SyncAsync()
    {
        DateTime lastCheckpoint = await GetLastCheckpointAsync();
        DateTime now = DateTime.UtcNow;

        using IServiceScope scope = _scopeFactory.CreateScope();
        IReportsRepository reportsRepository = scope.ServiceProvider.GetRequiredService<IReportsRepository>();
        IHeatmapRepository heatmapRepository = scope.ServiceProvider.GetRequiredService<IHeatmapRepository>();

        List<CitizenReport> newReports = await reportsRepository.GetReportsSinceAsync(lastCheckpoint, now);

        if (newReports.Count == 0)
        {
            return;
        }

        // Agrupar por (alertId, geohash) para celdas por alerta
        // y por geohash para celdas globales (alertId = null)
        int cellsUpdated = 0;

        IEnumerable<IGrouping<string, CitizenReport>> byAlertAndGeo = newReports
            .GroupBy(r => $"{r.AlertId}|{GeoHashHelper.Encode(r.Latitude, r.Longitude)}");

        foreach (IGrouping<string, CitizenReport> group in byAlertAndGeo)
        {
            List<CitizenReport> groupReports = group.ToList();
            string geohash = GeoHashHelper.Encode(groupReports[0].Latitude, groupReports[0].Longitude);
            (double centerLat, double centerLng) = GeoHashHelper.Decode(geohash);

            int awarenessCount = groupReports.Count(r => r.Type == ReportType.Awareness);
            int infoCount = groupReports.Count(r => r.Type == ReportType.Info);
            double intensity = (infoCount * _heatmapSettings.InfoPointWeight) +
                               (awarenessCount * _heatmapSettings.AwarenessPointWeight);

            string cellId = $"{groupReports[0].AlertId}|{geohash}";

            HeatmapCell cell = new HeatmapCell
            {
                Id = cellId,
                AlertId = groupReports[0].AlertId,
                CenterLatitude = centerLat,
                CenterLongitude = centerLng,
                AwarenessCount = awarenessCount,
                InfoCount = infoCount,
                WeightedIntensity = intensity,
                LastUpdated = now
            };

            await heatmapRepository.UpsertAsync(cell);
            cellsUpdated++;
        }

        // Celdas globales (sin filtro por alerta)
        IEnumerable<IGrouping<string, CitizenReport>> byGeoGlobal = newReports
            .GroupBy(r => GeoHashHelper.Encode(r.Latitude, r.Longitude));

        foreach (IGrouping<string, CitizenReport> group in byGeoGlobal)
        {
            List<CitizenReport> groupReports = group.ToList();
            string geohash = group.Key;
            (double centerLat, double centerLng) = GeoHashHelper.Decode(geohash);

            int awarenessCount = groupReports.Count(r => r.Type == ReportType.Awareness);
            int infoCount = groupReports.Count(r => r.Type == ReportType.Info);
            double intensity = (infoCount * _heatmapSettings.InfoPointWeight) +
                               (awarenessCount * _heatmapSettings.AwarenessPointWeight);

            HeatmapCell globalCell = new HeatmapCell
            {
                Id = $"global|{geohash}",
                AlertId = null,
                CenterLatitude = centerLat,
                CenterLongitude = centerLng,
                AwarenessCount = awarenessCount,
                InfoCount = infoCount,
                WeightedIntensity = intensity,
                LastUpdated = now
            };

            await heatmapRepository.UpsertAsync(globalCell);
            cellsUpdated++;
        }

        await UpdateCheckpointAsync(now);
        _logger.LogInformation(
            "HeatmapSync: procesados {Reports} reportes, actualizadas {Cells} celdas",
            newReports.Count, cellsUpdated);
    }

    private async Task<DateTime> GetLastCheckpointAsync()
    {
        Cassandra.ISession session = _cassandraFactory.GetSession();
        Cassandra.SimpleStatement statement = new Cassandra.SimpleStatement(
            $"SELECT last_processed_at FROM {_dbSettings.Cassandra.Keyspace}.sync_checkpoints WHERE service_name = ?",
            CheckpointKey);

        Cassandra.RowSet rows = await session.ExecuteAsync(statement);
        Cassandra.Row? row = rows.FirstOrDefault();

        if (row == null || row.IsNull("last_processed_at"))
            return DateTime.MinValue;

        return row.GetValue<DateTimeOffset>("last_processed_at").UtcDateTime;
    }

    private async Task UpdateCheckpointAsync(DateTime checkpoint)
    {
        Cassandra.ISession session = _cassandraFactory.GetSession();
        Cassandra.SimpleStatement statement = new Cassandra.SimpleStatement(
            $"INSERT INTO {_dbSettings.Cassandra.Keyspace}.sync_checkpoints (service_name, last_processed_at) VALUES (?, ?)",
            CheckpointKey, checkpoint);

        await session.ExecuteAsync(statement);
    }
}
