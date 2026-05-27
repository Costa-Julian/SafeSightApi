namespace SafeSight.Api.Models.Configuration;

// Para cambiar el entorno (local → Docker → producción):
// solo modificar los valores en appsettings.json o variables de entorno.
// Ningún archivo de código necesita tocarse.
public class DatabaseSettings
{
    public MongoDbSettings MongoDb { get; set; } = new();
    public CassandraSettings Cassandra { get; set; } = new();
}

public class MongoDbSettings
{
    // Soporta cualquier URI de MongoDB:
    //   Sin auth:    mongodb://host:27017
    //   Con auth:    mongodb://user:pass@host:27017
    //   Con auth DB: mongodb://user:pass@host:27017/?authSource=admin
    //   Atlas:       mongodb+srv://user:pass@cluster.mongodb.net
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "safesight";
}

public class CassandraSettings
{
    // Lista de hosts o IPs del cluster. Para Docker: el nombre del servicio o la IP del contenedor.
    public List<string> ContactPoints { get; set; } = new() { "localhost" };
    public int Port { get; set; } = 9042;
    public string Keyspace { get; set; } = "safesight";
    // Credenciales opcionales. Dejar vacío si Cassandra no tiene autenticación habilitada.
    public string? Username { get; set; }
    public string? Password { get; set; }
}
