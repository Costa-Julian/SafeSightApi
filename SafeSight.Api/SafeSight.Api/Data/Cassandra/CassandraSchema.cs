using Microsoft.Extensions.Logging;
using SafeSight.Api.Models.Configuration;

namespace SafeSight.Api.Data.Cassandra;

public class CassandraSchema
{
    private readonly global::Cassandra.ISession _session;
    private readonly CassandraSettings _settings;
    private readonly ILogger<CassandraSchema> _logger;

    public CassandraSchema(CassandraSessionFactory factory, DatabaseSettings settings, ILogger<CassandraSchema> logger)
    {
        _session = (global::Cassandra.ISession)factory.GetSession();
        _settings = settings.Cassandra;
        _logger = logger;
    }

    public async Task EnsureSchemaAsync()
    {
        await CreateKeyspaceAsync();
        await UseKeyspaceAsync();
        await CreateTablesAsync();
        _logger.LogInformation("Cassandra schema verificado en keyspace '{Keyspace}'", _settings.Keyspace);
    }

    private async Task CreateKeyspaceAsync()
    {
        // SimpleStrategy con replication_factor=1 es suficiente para desarrollo local.
        // En producción con cluster multi-nodo: cambiar a NetworkTopologyStrategy.
        string cql = $@"
            CREATE KEYSPACE IF NOT EXISTS {_settings.Keyspace}
            WITH REPLICATION = {{'class': 'SimpleStrategy', 'replication_factor': 1}}";
        await _session.ExecuteAsync(new global::Cassandra.SimpleStatement(cql));
    }

    private async Task UseKeyspaceAsync()
    {
        await _session.ExecuteAsync(new global::Cassandra.SimpleStatement($"USE {_settings.Keyspace}"));
    }

    private async Task CreateTablesAsync()
    {
        // Tabla principal de reportes ciudadanos, organizada para lecturas por alerta.
        // Partition key: (alert_id, time_bucket)
        //   - alert_id para agrupar reportes de la misma alerta
        //   - time_bucket (YYYY-MM-DD) evita hot partitions en alertas con muchos reportes
        // Clustering key: reported_at ASC, id ASC
        //   - reported_at para consultas por rango de tiempo
        //   - id como desempate (unicidad de fila)
        await _session.ExecuteAsync(new global::Cassandra.SimpleStatement(@"
            CREATE TABLE IF NOT EXISTS citizen_reports (
                alert_id    TEXT,
                time_bucket TEXT,
                reported_at TIMESTAMP,
                id          UUID,
                type        INT,
                latitude    DOUBLE,
                longitude   DOUBLE,
                description TEXT,
                photo_url   TEXT,
                PRIMARY KEY ((alert_id, time_bucket), reported_at, id)
            ) WITH CLUSTERING ORDER BY (reported_at ASC, id ASC)"));

        // Tabla secundaria para el HeatmapSyncService.
        // Partition key: time_bucket (solo fecha)
        //   - permite escanear todos los reportes de un día sin conocer alert_id
        //   - el SyncService consulta: WHERE time_bucket = ? AND reported_at > ?
        // Esta es una denormalización deliberada (dual-write) para hacer eficiente la lectura del sync.
        await _session.ExecuteAsync(new global::Cassandra.SimpleStatement(@"
            CREATE TABLE IF NOT EXISTS citizen_reports_by_time (
                time_bucket TEXT,
                reported_at TIMESTAMP,
                alert_id    TEXT,
                id          UUID,
                type        INT,
                latitude    DOUBLE,
                longitude   DOUBLE,
                PRIMARY KEY (time_bucket, reported_at, id)
            ) WITH CLUSTERING ORDER BY (reported_at ASC, id ASC)"));

        // Checkpoint para el HeatmapSyncService.
        // Guarda el último reported_at procesado para lecturas incrementales.
        // El timestamp en Cassandra tiene resolución de milisegundos.
        await _session.ExecuteAsync(new global::Cassandra.SimpleStatement(@"
            CREATE TABLE IF NOT EXISTS sync_checkpoints (
                service_name    TEXT PRIMARY KEY,
                last_processed_at TIMESTAMP
            )"));
    }
}
