using Cassandra;
using SafeSight.Api.Data.Cassandra;
using SafeSight.Api.Models.Configuration;
using SafeSight.Api.Models.Domain;
using SafeSight.Api.Models.Dtos.Responses;
using SafeSight.Api.Repositories.Interfaces;

namespace SafeSight.Api.Repositories;

public class ReportsRepository : IReportsRepository
{
    private readonly Cassandra.ISession _session;
    private readonly string _keyspace;

    public ReportsRepository(CassandraSessionFactory factory, DatabaseSettings settings)
    {
        _session = factory.GetSession();
        _keyspace = settings.Cassandra.Keyspace;
    }

    public async Task InsertAsync(CitizenReport report)
    {
        string timeBucket = GetTimeBucket(report.ReportedAt);

        // TODO: en producción usar PreparedStatement para mejor performance.
        SimpleStatement mainInsert = new SimpleStatement(
            $"INSERT INTO {_keyspace}.citizen_reports " +
            "(alert_id, time_bucket, reported_at, id, type, citizen_id, latitude, longitude, description, photo_url) " +
            "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
            report.AlertId, timeBucket, report.ReportedAt, report.Id,
            (int)report.Type, report.CitizenId, report.Latitude, report.Longitude,
            report.Description, report.PhotoUrl);

        SimpleStatement heatmapSyncInsert = new SimpleStatement(
            $"INSERT INTO {_keyspace}.citizen_reports_by_time " +
            "(time_bucket, reported_at, alert_id, id, type, latitude, longitude) " +
            "VALUES (?, ?, ?, ?, ?, ?, ?)",
            timeBucket, report.ReportedAt, report.AlertId, report.Id,
            (int)report.Type, report.Latitude, report.Longitude);

        await _session.ExecuteAsync(mainInsert);
        await _session.ExecuteAsync(heatmapSyncInsert);

        if (report.Type == ReportType.Info)
        {
            SimpleStatement infoSyncInsert = new SimpleStatement(
                $"INSERT INTO {_keyspace}.info_reports_sync " +
                "(time_bucket, reported_at, id, alert_id, citizen_id, latitude, longitude, description, photo_url) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
                timeBucket, report.ReportedAt, report.Id, report.AlertId,
                report.CitizenId, report.Latitude, report.Longitude,
                report.Description, report.PhotoUrl);

            await _session.ExecuteAsync(infoSyncInsert);
        }
    }

    public async Task<PagedResponse<CitizenReport>> GetByAlertAsync(string alertId, int page, int pageSize)
    {
        // Consulta los últimos 30 días de time buckets para esta alerta.
        // MVP: paginación simple sin Cassandra paging state persistido entre requests.
        // TODO: implementar cursor-based pagination con paging state del driver para datasets grandes.
        List<CitizenReport> allReports = new List<CitizenReport>();

        for (int daysAgo = 29; daysAgo >= 0; daysAgo--)
        {
            string timeBucket = GetTimeBucket(DateTime.UtcNow.AddDays(-daysAgo));
            SimpleStatement statement = new SimpleStatement(
                $"SELECT * FROM {_keyspace}.citizen_reports WHERE alert_id = ? AND time_bucket = ?",
                alertId, timeBucket);

            RowSet rows = await _session.ExecuteAsync(statement);
            foreach (Row row in rows)
            {
                allReports.Add(MapRowToReport(row));
            }
        }

        allReports = allReports.OrderByDescending(r => r.ReportedAt).ToList();
        int total = allReports.Count;
        List<CitizenReport> pageItems = allReports
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResponse<CitizenReport>
        {
            Items = pageItems,
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize)
        };
    }

    public async Task<List<CitizenReport>> GetReportsSinceAsync(DateTime since, DateTime until)
    {
        List<CitizenReport> allReports = new List<CitizenReport>();

        // Limitar la ventana a los últimos 30 días para evitar escanear demasiados buckets.
        DateTime effectiveSince = since < DateTime.UtcNow.AddDays(-30)
            ? DateTime.UtcNow.AddDays(-30)
            : since;

        DateTime current = effectiveSince.Date;
        DateTime endDate = until.Date;

        while (current <= endDate)
        {
            string timeBucket = GetTimeBucket(current);
            SimpleStatement statement = new SimpleStatement(
                $"SELECT * FROM {_keyspace}.citizen_reports_by_time " +
                "WHERE time_bucket = ? AND reported_at > ? AND reported_at <= ?",
                timeBucket, effectiveSince, until);

            RowSet rows = await _session.ExecuteAsync(statement);
            foreach (Row row in rows)
            {
                allReports.Add(MapSyncRowToReport(row));
            }

            current = current.AddDays(1);
        }

        return allReports;
    }

    public async Task<long> CountByAlertAsync(string alertId)
    {
        long total = 0;
        for (int daysAgo = 29; daysAgo >= 0; daysAgo--)
        {
            string timeBucket = GetTimeBucket(DateTime.UtcNow.AddDays(-daysAgo));
            SimpleStatement statement = new SimpleStatement(
                $"SELECT COUNT(*) FROM {_keyspace}.citizen_reports WHERE alert_id = ? AND time_bucket = ?",
                alertId, timeBucket);

            RowSet rows = await _session.ExecuteAsync(statement);
            Row? row = rows.FirstOrDefault();
            if (row != null)
            {
                total += row.GetValue<long>("count");
            }
        }
        return total;
    }

    private static string GetTimeBucket(DateTime dateTime) => dateTime.ToUniversalTime().ToString("yyyy-MM-dd");

    private static CitizenReport MapRowToReport(Row row)
    {
        return new CitizenReport
        {
            Id = row.GetValue<Guid>("id"),
            AlertId = row.GetValue<string>("alert_id"),
            Type = (ReportType)row.GetValue<int>("type"),
            Latitude = row.GetValue<double>("latitude"),
            Longitude = row.GetValue<double>("longitude"),
            ReportedAt = row.GetValue<DateTimeOffset>("reported_at").UtcDateTime,
            Description = row.IsNull("description") ? null : row.GetValue<string>("description"),
            PhotoUrl = row.IsNull("photo_url") ? null : row.GetValue<string>("photo_url")
        };
    }

    private static CitizenReport MapSyncRowToReport(Row row)
    {
        return new CitizenReport
        {
            Id = row.GetValue<Guid>("id"),
            AlertId = row.GetValue<string>("alert_id"),
            Type = (ReportType)row.GetValue<int>("type"),
            Latitude = row.GetValue<double>("latitude"),
            Longitude = row.GetValue<double>("longitude"),
            ReportedAt = row.GetValue<DateTimeOffset>("reported_at").UtcDateTime
        };
    }
}
