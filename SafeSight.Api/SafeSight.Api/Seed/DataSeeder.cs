using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using SafeSight.Api.Data.Mongo;
using SafeSight.Api.Models.Domain;
using SafeSight.Api.Repositories.Interfaces;

namespace SafeSight.Api.Seed;

public class DataSeeder
{
    private readonly SafeSightDbContext _dbContext;
    private readonly IReportsRepository _reportsRepository;
    private readonly ILogger<DataSeeder> _logger;

    // Ubicaciones ficticias del AMBA para los datos de prueba
    private static readonly (string City, double Lat, double Lng)[] Locations =
    {
        ("CABA",        -34.6037, -58.3816),
        ("La Matanza",  -34.7756, -58.5064),
        ("San Isidro",  -34.4631, -58.5212),
        ("Quilmes",     -34.7217, -58.2526)
    };

    public DataSeeder(SafeSightDbContext dbContext, IReportsRepository reportsRepository, ILogger<DataSeeder> logger)
    {
        _dbContext = dbContext;
        _reportsRepository = reportsRepository;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        bool hasAlerts = await _dbContext.Alerts.AnyAsync();
        if (hasAlerts)
        {
            _logger.LogInformation("Base de datos ya tiene datos. Seed omitido.");
            return;
        }

        _logger.LogInformation("Iniciando seed de datos...");

        List<Alert> alerts = CreateAlerts();
        foreach (Alert alert in alerts)
        {
            alert.Id = ObjectId.GenerateNewId().ToString();
            _dbContext.Alerts.Add(alert);
        }
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Seed: {Count} alertas creadas en MongoDB", alerts.Count);

        await SeedReportsAsync(alerts);
        _logger.LogInformation("Seed completado. Los reportes serán procesados por HeatmapSyncService.");
    }

    private static List<Alert> CreateAlerts()
    {
        return new List<Alert>
        {
            new Alert
            {
                MissingPerson = new MissingPerson
                {
                    FirstName = "Valentina",
                    LastName = "Ríos",
                    Age = 9,
                    PhysicalDescription = "Niña de cabello castaño largo, ojos verdes, 1.25m, tez blanca. Última vez vestía remera rosa y zapatillas blancas.",
                    PhotoUrl = "https://i.pravatar.cc/300?img=47"
                },
                Situation = "Vista por última vez en el Parque Centenario. Se alejó sola mientras jugaba. No responde a llamados.",
                LastKnownLocation = new GeoPoint { Latitude = Locations[0].Lat, Longitude = Locations[0].Lng },
                DisappearanceDate = DateTime.UtcNow.AddDays(-2),
                EmitterId = (int)EmitterType.Citizen,
                EmittedAt = DateTime.UtcNow.AddHours(-47),
                Status = AlertStatus.Active
            },
            new Alert
            {
                MissingPerson = new MissingPerson
                {
                    FirstName = "Tomás",
                    LastName = "Medina",
                    Age = 17,
                    PhysicalDescription = "Adolescente, 1.75m, cabello negro corto, ojos marrones. Llevaba campera azul y jeans negros.",
                    PhotoUrl = "https://i.pravatar.cc/300?img=12"
                },
                Situation = "No regresó a su domicilio después de salir a ver a amigos el viernes por la noche.",
                LastKnownLocation = new GeoPoint { Latitude = Locations[1].Lat, Longitude = Locations[1].Lng },
                DisappearanceDate = DateTime.UtcNow.AddDays(-3),
                EmitterId = (int)EmitterType.Entity,
                EmittedAt = DateTime.UtcNow.AddHours(-70),
                Status = AlertStatus.Active
            },
            new Alert
            {
                MissingPerson = new MissingPerson
                {
                    FirstName = "Claudia",
                    LastName = "Ferreyra",
                    Age = 42,
                    PhysicalDescription = "Mujer, 1.62m, cabello rubio, ojos celestes. Viste uniforme de enfermería blanco.",
                    PhotoUrl = "https://i.pravatar.cc/300?img=32"
                },
                Situation = "No llegó a su turno de trabajo en el hospital. Su celular está apagado desde las 6am.",
                LastKnownLocation = new GeoPoint { Latitude = Locations[2].Lat, Longitude = Locations[2].Lng },
                DisappearanceDate = DateTime.UtcNow.AddDays(-1),
                EmitterId = (int)EmitterType.Entity,
                EmittedAt = DateTime.UtcNow.AddHours(-22),
                Status = AlertStatus.Active
            },
            new Alert
            {
                MissingPerson = new MissingPerson
                {
                    FirstName = "Ernesto",
                    LastName = "Sánchez",
                    Age = 78,
                    PhysicalDescription = "Adulto mayor, 1.68m, cabello blanco, usa anteojos. Viste pantalón gris y chomba verde. Camina con bastón.",
                    PhotoUrl = "https://i.pravatar.cc/300?img=67"
                },
                Situation = "Salió de su casa por medicamentos y no regresó. Tiene principio de demencia.",
                LastKnownLocation = new GeoPoint { Latitude = Locations[3].Lat, Longitude = Locations[3].Lng },
                DisappearanceDate = DateTime.UtcNow.AddDays(-1),
                EmitterId = (int)EmitterType.Citizen,
                EmittedAt = DateTime.UtcNow.AddHours(-18),
                Status = AlertStatus.Active
            }
        };
    }

    private async Task SeedReportsAsync(List<Alert> alerts)
    {
        Random rng = new Random(42);
        int totalReports = 0;

        foreach (Alert alert in alerts)
        {
            double baseLat = alert.LastKnownLocation.Latitude;
            double baseLng = alert.LastKnownLocation.Longitude;

            int awarenessCount = rng.Next(45, 65);
            int infoCount = rng.Next(15, 25);

            // Awareness: puntos dispersos en radio amplio (~5 km)
            for (int i = 0; i < awarenessCount; i++)
            {
                double offsetLat = (rng.NextDouble() - 0.5) * 0.09;
                double offsetLng = (rng.NextDouble() - 0.5) * 0.09;

                CitizenReport report = new CitizenReport
                {
                    Id = Guid.NewGuid(),
                    AlertId = alert.Id,
                    Type = ReportType.Awareness,
                    Latitude = baseLat + offsetLat,
                    Longitude = baseLng + offsetLng,
                    ReportedAt = DateTime.UtcNow.AddMinutes(-rng.Next(10, 2880))
                };
                await _reportsRepository.InsertAsync(report);
                totalReports++;
            }

            // Info: puntos concentrados en radio menor (~2 km), simulando una "ruta"
            double routeOffsetLat = (rng.NextDouble() - 0.5) * 0.02;
            double routeOffsetLng = (rng.NextDouble() - 0.5) * 0.02;

            for (int i = 0; i < infoCount; i++)
            {
                double spreadLat = (rng.NextDouble() - 0.5) * 0.03;
                double spreadLng = (rng.NextDouble() - 0.5) * 0.03;

                CitizenReport report = new CitizenReport
                {
                    Id = Guid.NewGuid(),
                    AlertId = alert.Id,
                    Type = ReportType.Info,
                    Latitude = baseLat + routeOffsetLat + spreadLat,
                    Longitude = baseLng + routeOffsetLng + spreadLng,
                    ReportedAt = DateTime.UtcNow.AddMinutes(-rng.Next(10, 1440)),
                    Description = GenerateInfoDescription(rng)
                };
                await _reportsRepository.InsertAsync(report);
                totalReports++;
            }
        }

        _logger.LogInformation("Seed: {Count} reportes ciudadanos insertados en Cassandra", totalReports);
    }

    private static string GenerateInfoDescription(Random rng)
    {
        string[] descriptions =
        {
            "Vi a una persona con esas características caminando sola por la zona.",
            "Creo haberla visto en el kiosco de la esquina hace un rato.",
            "Una persona similar estuvo en la parada de colectivo.",
            "La vi entrar a una farmacia en esta dirección.",
            "Vi a alguien similar en el supermercado, lucía confundida.",
            "Pasó caminando frente a mi trabajo, parecía desorientada.",
            "La vi sentada en una plaza cercana.",
            "Subió al colectivo 15 en este punto."
        };
        return descriptions[rng.Next(descriptions.Length)];
    }
}
