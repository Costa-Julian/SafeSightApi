using FirebaseAdmin;
using FluentValidation;
using Google.Apis.Auth.OAuth2;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System.Globalization;
using SafeSight.Api.BackgroundServices;
using SafeSight.Api.Data.Cassandra;
using SafeSight.Api.Data.Mongo;
using SafeSight.Api.Models.Configuration;
using SafeSight.Api.Repositories;
using SafeSight.Api.Repositories.Interfaces;
using SafeSight.Api.Seed;
using SafeSight.Api.Services;
using SafeSight.Api.Services.Interfaces;
using System.Text.Json.Serialization;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ── Configuración fuertemente tipada ──────────────────────────────────────────
// Para mover las bases a Docker: solo cambiar las cadenas en appsettings.json.
DatabaseSettings databaseSettings = builder.Configuration
    .GetSection("DatabaseSettings")
    .Get<DatabaseSettings>() ?? new DatabaseSettings();

builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("DatabaseSettings"));
builder.Services.Configure<SyncSettings>(builder.Configuration.GetSection("SyncSettings"));
builder.Services.Configure<HeatmapSettings>(builder.Configuration.GetSection("HeatmapSettings"));
builder.Services.Configure<SeedSettings>(builder.Configuration.GetSection("SeedSettings"));
builder.Services.Configure<PhotoSettings>(builder.Configuration.GetSection("PhotoSettings"));

builder.Services.AddSingleton(databaseSettings);

// ── MongoDB (EF Core) ─────────────────────────────────────────────────────────
builder.Services.AddDbContext<SafeSightDbContext>(options =>
    options.UseMongoDB(
        databaseSettings.MongoDb.ConnectionString,
        databaseSettings.MongoDb.DatabaseName));

builder.Services.AddSingleton<IMongoDatabase>(_ =>
{
    MongoClient client = new MongoClient(databaseSettings.MongoDb.ConnectionString);
    return client.GetDatabase(databaseSettings.MongoDb.DatabaseName);
});

builder.Services.AddSingleton<MongoConfiguration>();

// ── Cassandra ─────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<CassandraSessionFactory>();
builder.Services.AddSingleton<CassandraSchema>();

// ── Firebase Admin SDK ────────────────────────────────────────────────────────
string firebaseCredsPath = Path.Combine(builder.Environment.ContentRootPath, "firebase-adminsdk.json");
if (File.Exists(firebaseCredsPath))
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseCredsPath)
    });
    Console.WriteLine("Firebase Admin SDK inicializado.");
}
else
{
    Console.WriteLine("ADVERTENCIA: firebase-adminsdk.json no encontrado. Las notificaciones push no funcionarán.");
}

// ── Repositorios (Scoped) ─────────────────────────────────────────────────────
builder.Services.AddScoped<IAlertsRepository, AlertsRepository>();
builder.Services.AddScoped<IReportsRepository, ReportsRepository>();
builder.Services.AddScoped<IHeatmapRepository, HeatmapRepository>();
builder.Services.AddScoped<IInfoReportsMongoRepository, InfoReportsMongoRepository>();
builder.Services.AddScoped<IStatsRepository, StatsRepository>();
builder.Services.AddScoped<IDeviceTokenRepository, DeviceTokenRepository>();

// ── Servicios (Scoped) ────────────────────────────────────────────────────────
builder.Services.AddScoped<IAlertsService, AlertsService>();
builder.Services.AddScoped<IReportsService, ReportsService>();
builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddScoped<IPhotoStorageService, PhotoStorageService>();
builder.Services.AddScoped<IFcmService, FcmService>();
builder.Services.AddScoped<DataSeeder>();

// ── Controllers + JSON ────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ── FluentValidation ──────────────────────────────────────────────────────────
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ── OpenAPI / Swagger ─────────────────────────────────────────────────────────
// Swashbuckle 10.x se apoya en el soporte nativo de .NET 10 (Microsoft.AspNetCore.OpenApi).
builder.Services.AddOpenApi();

// ── CORS (abierto para MVP — restringir en producción) ────────────────────────
builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ── Background Services ───────────────────────────────────────────────────────
builder.Services.AddHostedService<HeatmapSyncService>();
builder.Services.AddHostedService<InfoReportSyncService>();

WebApplication app = builder.Build();

// ── Middleware de manejo de errores ───────────────────────────────────────────
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            data = (object?)null,
            error = "Ocurrió un error interno. Por favor intente nuevamente."
        });
    });
});

// ── OpenAPI spec + Swagger UI ─────────────────────────────────────────────────
app.MapOpenApi();
app.UseSwaggerUI(opts =>
{
    opts.SwaggerEndpoint("/openapi/v1.json", "SafeSight API v1");
    opts.RoutePrefix = "swagger";
});

app.UseStaticFiles();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

// ── Startup: schema, índices y seed ──────────────────────────────────────────
// Corre antes de app.Run() para garantizar que los datos estén listos
// cuando el HeatmapSyncService arranque con el host.
using (IServiceScope startupScope = app.Services.CreateScope())
{
    ILogger<Program> startupLogger = startupScope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        CassandraSchema cassandraSchema = startupScope.ServiceProvider.GetRequiredService<CassandraSchema>();
        await cassandraSchema.EnsureSchemaAsync();
    }
    catch (Exception ex)
    {
        ILogger<Program> logger = startupScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al inicializar el schema de Cassandra. Verifique que Cassandra esté accesible en {ContactPoints}",
            string.Join(", ", databaseSettings.Cassandra.ContactPoints));
    }

    try
    {
        MongoConfiguration mongoConfig = startupScope.ServiceProvider.GetRequiredService<MongoConfiguration>();
        await mongoConfig.EnsureIndexesAsync();
    }
    catch (Exception ex)
    {
        ILogger<Program> logger = startupScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al inicializar índices de MongoDB. Verifique que MongoDB esté accesible en {ConnectionString}",
            databaseSettings.MongoDb.ConnectionString);
    }

    SeedSettings seedSettings = startupScope.ServiceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<SeedSettings>>().Value;

    if (seedSettings.EnableSeedOnStartup)
    {
        try
        {
            DataSeeder seeder = startupScope.ServiceProvider.GetRequiredService<DataSeeder>();
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            ILogger<Program> logger = startupScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Error durante el seed de datos");
        }
    }
}

app.Run();
