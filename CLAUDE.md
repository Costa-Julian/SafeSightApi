# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

SafeSight API — REST backend (ASP.NET Core, .NET 10) for a collaborative missing-persons search platform that complements Argentina's official "Alerta Sofía" system. Academic Data Engineering project. Code comments, docs, and log messages are in Spanish; keep that convention when editing.

## Directory layout

The repo nests one level deeper than usual:
- Repo root: `SafeSightApi/`
- Solution + docs + README: `SafeSight.Api/` (contains `SafeSight.Api.slnx`, `README.md`, `docs/`)
- Actual project: `SafeSight.Api/SafeSight.Api/` (contains `SafeSight.Api.csproj`, `Program.cs`)

## Commands

Run all from `SafeSight.Api/SafeSight.Api/` (the project dir):

```bash
dotnet run        # starts API on http://0.0.0.0:5121 (Swagger at /swagger)
dotnet build
dotnet watch run  # hot reload during development
```

There is no test project and no Dockerfile/compose in the repo (despite `appsettings.Docker.json` existing for when DBs move to containers).

## Required infrastructure

The API needs two databases running before it starts (it logs errors but still boots if they're unreachable):
- **MongoDB** on `localhost:27017`
- **Apache Cassandra** on `localhost:9042`

Connection strings live in `appsettings.json` under `DatabaseSettings`. Moving DBs to Docker means changing only those strings — no code changes.

On startup `Program.cs` runs, in order: Cassandra schema creation (`CassandraSchema.EnsureSchemaAsync`), Mongo index creation (`MongoConfiguration.EnsureIndexesAsync`), optional data seeding (`DataSeeder`, gated by `SeedSettings.EnableSeedOnStartup`, default true — seeds ~4 alerts + ~240 reports if DBs are empty), then `ActivityMigration`.

## Core architecture: polyglot persistence with eventual consistency

This is the central design idea — understand it before touching repositories or background services.

**Cassandra = write-optimized ingestion (source of truth for raw events).** Absorbs the flood of citizen reports. A single citizen report is written to up to three tables in `ReportsRepository.InsertAsync` (a deliberate denormalized dual/triple-write):
- `citizen_reports` — partition key `(alert_id, time_bucket)`, for reads by alert
- `citizen_reports_by_time` — partition key `time_bucket`, lets sync services scan a day without knowing alert IDs
- `info_reports_sync` — staging table, written only for `ReportType.Info`

`time_bucket` is a `YYYY-MM-DD` string used as part of partition keys to avoid hot partitions. `sync_checkpoints` stores each background service's last-processed timestamp for incremental reads.

**MongoDB = read-optimized analytics (pre-aggregated Data Mart).** Collections: `alerts` (master data, EF Core), `heatmap_cells`, `info_reports`, `admin_activity`, `device_tokens`.

**Background services bridge the two with eventual consistency** (`BackgroundServices/`, registered as hosted services, run every `SyncSettings.HeatmapSyncIntervalSeconds` = 15s):
- `HeatmapSyncService` — reads new Cassandra reports since checkpoint, groups by geohash (~1 km² cells), computes `WeightedIntensity = InfoCount×3 + AwarenessCount×1`, upserts `heatmap_cells` in Mongo. Cell `_id` is `"{alertId}|{geohash}"` or `"global|{geohash}"`.
- `InfoReportSyncService` — migrates `info_reports_sync` rows to the Mongo `info_reports` collection.

Consequence baked into the API contract: `GET /api/stats/heatmap` and `GET /api/reports/info/by-alert/{id}` always read pre-aggregated Mongo data and are **~15s behind** Cassandra. This lag is intentional, not a bug. The heatmap is never computed on the fly. Reading raw both-types reports (`GET /api/reports/by-alert/{id}`) goes to Cassandra directly.

## Layering and conventions

Request flow: **Controller → Service (interface in `Services/Interfaces/`) → Repository (interface in `Repositories/Interfaces/`) → DB.** Services and repositories are registered `Scoped` in `Program.cs`; DB clients/factories and config are `Singleton`.

- **Every endpoint returns the `ApiResult<T>` wrapper** (`Common/ApiResult.cs`): `{ success, data, error }`. Use `ApiResult<T>.Ok(data)` / `.Fail(error)`. The global exception handler in `Program.cs` emits the same shape on 500s. Paginated payloads use `PagedResponse<T>`.
- **Validation** is FluentValidation, auto-applied (`Validators/`, registered via `AddValidatorsFromAssemblyContaining<Program>`). Add a validator class per request DTO.
- **Enums** serialize as strings (`JsonStringEnumConverter`) — except `ReportType`, which is documented/used as int (`0` = Awareness, `1` = Info). `AlertStatus`: `Active`/`Resolved`/`Cancelled`.
- **Timestamps** are stored UTC; services call `.ToUniversalTime()` on incoming dates.
- **Photos**: multipart uploads handled by `PhotoStorageService`, saved to disk and served as static files from `/photos/...`; DTOs carry the relative `photoUrl`. Max size from `PhotoSettings.MaxFileSizeMb` (default 5).
- **Admin activity feed**: creating alerts, submitting reports, and changing alert status write an `AdminActivity` document (`Kind`: `AlertCreated` / `ReportSubmitted` / `StatusChanged` / `System`) for the admin dashboard.
- **Push notifications**: Firebase Admin SDK, initialized only if `firebase-adminsdk.json` exists at the content root (gitignored); otherwise FCM silently no-ops with a startup warning. `FcmService` + `device_tokens`.

## Reference docs

- `SafeSight.Api/docs/API.md` — full endpoint reference (request/response shapes).
- `SafeSight.Api/docs/DB_SCHEMA.md` — exact Cassandra CQL + Mongo index definitions the API creates on startup.
- `SafeSight.Api/README.md` — setup and the data-architecture rationale (Spanish).
