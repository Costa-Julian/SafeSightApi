using Microsoft.Extensions.Logging;
using SafeSight.Api.Models.Configuration;

namespace SafeSight.Api.Data.Cassandra;

// Singleton: una sola ISession compartida por toda la app.
// Conexión lazy: se establece la primera vez que GetSession() es llamado,
// no al construir el objeto. La app arranca aunque Cassandra no esté disponible;
// los endpoints que usen Cassandra fallarán con un error claro en los logs.
// Para cluster multi-nodo: agregar más entradas en ContactPoints.
public class CassandraSessionFactory : IDisposable
{
    private readonly CassandraSettings _settings;
    private readonly ILogger<CassandraSessionFactory> _logger;
    private readonly object _lock = new();
    private global::Cassandra.ISession? _session;
    private bool _disposed;

    public CassandraSessionFactory(DatabaseSettings settings, ILogger<CassandraSessionFactory> logger)
    {
        _settings = settings.Cassandra;
        _logger = logger;
    }

    public global::Cassandra.ISession GetSession()
    {
        if (_session != null) return _session;

        lock (_lock)
        {
            if (_session != null) return _session;

            global::Cassandra.Builder clusterBuilder = global::Cassandra.Cluster.Builder();

            foreach (string contactPoint in _settings.ContactPoints)
            {
                clusterBuilder.AddContactPoint(contactPoint);
            }

            clusterBuilder.WithPort(_settings.Port);

            if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                clusterBuilder.WithCredentials(_settings.Username, _settings.Password ?? string.Empty);
            }

            global::Cassandra.ICluster cluster = clusterBuilder.Build();
            _session = cluster.Connect();
            _logger.LogInformation("Cassandra session establecida: {ContactPoints}:{Port}",
                string.Join(",", _settings.ContactPoints), _settings.Port);
        }

        return _session;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _session?.Dispose();
            _disposed = true;
        }
    }
}
