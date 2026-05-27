using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SafeSight.Api.Models.Domain;

namespace SafeSight.Api.Data.Mongo;

public class MongoConfiguration
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoConfiguration> _logger;

    public MongoConfiguration(IMongoDatabase database, ILogger<MongoConfiguration> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task EnsureIndexesAsync()
    {
        await EnsureAlertIndexesAsync();
        await EnsureHeatmapIndexesAsync();
        _logger.LogInformation("MongoDB indexes verified");
    }

    private async Task EnsureAlertIndexesAsync()
    {
        IMongoCollection<Alert> collection = _database.GetCollection<Alert>("alerts");

        // Índice en Status para listar alertas activas eficientemente
        IndexKeysDefinition<Alert> statusIndex = Builders<Alert>.IndexKeys.Ascending("Status");
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Alert>(statusIndex, new CreateIndexOptions { Name = "idx_alerts_status" }));

        // Índice compuesto (EmitterId, Status) para el endpoint by-emitter
        IndexKeysDefinition<Alert> emitterIndex = Builders<Alert>.IndexKeys
            .Ascending("EmitterId")
            .Ascending("Status");
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Alert>(emitterIndex, new CreateIndexOptions { Name = "idx_alerts_emitter_status" }));

        // Nota: el índice 2dsphere para consultas geoespaciales requeriría que LastKnownLocation
        // esté en formato GeoJSON { type: "Point", coordinates: [lng, lat] }.
        // Para el MVP con la estructura GeoPoint actual se omite; se agrega en la evolución del sistema.
    }

    private async Task EnsureHeatmapIndexesAsync()
    {
        IMongoCollection<HeatmapCell> collection = _database.GetCollection<HeatmapCell>("heatmap_cells");

        // Índice en AlertId para filtrar celdas por alerta
        IndexKeysDefinition<HeatmapCell> alertIndex = Builders<HeatmapCell>.IndexKeys.Ascending("AlertId");
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<HeatmapCell>(alertIndex, new CreateIndexOptions { Name = "idx_heatmap_alertid" }));
    }
}
