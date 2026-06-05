using Cassandra;
using Microsoft.Extensions.Options;
using SafeSight.Api.Data.Cassandra;
using SafeSight.Api.Models.Configuration;
using SafeSight.Api.Models.Domain;
using SafeSight.Api.Repositories.Interfaces;

namespace SafeSight.Api.BackgroundServices;

// Pipeline: Cassandra info_reports_sync → MongoDB info_reports
// Consistencia eventual: los reportes con descripción y foto llegan a MongoDB
// con un retraso máximo de HeatmapSyncIntervalSeconds segundos.
public class InfoReportSyncService : BackgroundService
{
    private const string CheckpointKey = "info_report_sync";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CassandraSessionFactory _cassandraFactory;
    private readonly SyncSettings _syncSettings;
    private readonly DatabaseSettings _dbSettings;
    private readonly ILogger<InfoReportSyncService> _logger;

    public InfoReportSyncService(
        IServiceScopeFactory scopeFactory,
        CassandraSessionFactory cassandraFactory,
        IOptions<SyncSettings> syncSettings,
        IOptions<DatabaseSettings> dbSettings,
        ILogger<InfoReportSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _cassandraFactory = cassandraFactory;
        _syncSettings = syncSettings.Value;
        _dbSettings = dbSettings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InfoReportSyncService iniciado. Intervalo: {Interval}s", _syncSettings.HeatmapSyncIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la sincronización de info reports");
            }

            await Task.Delay(TimeSpan.FromSeconds(_syncSettings.HeatmapSyncIntervalSeconds), stoppingToken);
        }
    }

    private async Task SyncAsync()
    {
        DateTime lastCheckpoint = await GetLastCheckpointAsync();
        DateTime now = DateTime.UtcNow;

        Cassandra.ISession session = _cassandraFactory.GetSession();
        string keyspace = _dbSettings.Cassandra.Keyspace;

        List<InfoReportDocument> newReports = new List<InfoReportDocument>();

        DateTime effectiveSince = lastCheckpoint < DateTime.UtcNow.AddDays(-30)
            ? DateTime.UtcNow.AddDays(-30)
            : lastCheckpoint;

        DateTime current = effectiveSince.Date;
        while (current <= now.Date)
        {
            string timeBucket = current.ToString("yyyy-MM-dd");
            SimpleStatement statement = new SimpleStatement(
                $"SELECT * FROM {keyspace}.info_reports_sync " +
                "WHERE time_bucket = ? AND reported_at > ? AND reported_at <= ?",
                timeBucket, effectiveSince, now);

            RowSet rows = await session.ExecuteAsync(statement);
            foreach (Row row in rows)
                newReports.Add(MapRow(row));

            current = current.AddDays(1);
        }

        if (newReports.Count == 0)
            return;

        using IServiceScope scope = _scopeFactory.CreateScope();
        IInfoReportsMongoRepository repo = scope.ServiceProvider.GetRequiredService<IInfoReportsMongoRepository>();

        foreach (InfoReportDocument doc in newReports)
            await repo.UpsertAsync(doc);

        await UpdateCheckpointAsync(now);
        _logger.LogInformation("InfoReportSync: sincronizados {Count} reportes a MongoDB", newReports.Count);
    }

    private async Task<DateTime> GetLastCheckpointAsync()
    {
        Cassandra.ISession session = _cassandraFactory.GetSession();
        SimpleStatement statement = new SimpleStatement(
            $"SELECT last_processed_at FROM {_dbSettings.Cassandra.Keyspace}.sync_checkpoints WHERE service_name = ?",
            CheckpointKey);

        RowSet rows = await session.ExecuteAsync(statement);
        Row? row = rows.FirstOrDefault();

        if (row == null || row.IsNull("last_processed_at"))
            return DateTime.MinValue;

        return row.GetValue<DateTimeOffset>("last_processed_at").UtcDateTime;
    }

    private async Task UpdateCheckpointAsync(DateTime checkpoint)
    {
        Cassandra.ISession session = _cassandraFactory.GetSession();
        SimpleStatement statement = new SimpleStatement(
            $"INSERT INTO {_dbSettings.Cassandra.Keyspace}.sync_checkpoints (service_name, last_processed_at) VALUES (?, ?)",
            CheckpointKey, checkpoint);

        await session.ExecuteAsync(statement);
    }

    private static InfoReportDocument MapRow(Row row) => new InfoReportDocument
    {
        Id = row.GetValue<Guid>("id"),
        AlertId = row.GetValue<string>("alert_id"),
        CitizenId = row.IsNull("citizen_id") ? string.Empty : row.GetValue<string>("citizen_id"),
        Latitude = row.GetValue<double>("latitude"),
        Longitude = row.GetValue<double>("longitude"),
        Description = row.IsNull("description") ? string.Empty : row.GetValue<string>("description"),
        PhotoUrl = row.IsNull("photo_url") ? null : row.GetValue<string>("photo_url"),
        ReportedAt = row.GetValue<DateTimeOffset>("reported_at").UtcDateTime
    };
}
