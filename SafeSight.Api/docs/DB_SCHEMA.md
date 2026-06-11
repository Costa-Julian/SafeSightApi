# SafeSight — Estructura de bases de datos

Este archivo documenta exactamente qué estructuras crea la API al arrancar por primera vez contra
las bases de datos. Sirve para pre-crear esas estructuras en un entorno Docker desde cero.

La API ejecuta esta inicialización automáticamente en el startup mediante `CassandraSchema.EnsureSchemaAsync()`
y `MongoConfiguration.EnsureIndexesAsync()`. Si preferís crearlas a mano antes de levantar la API,
podés usar los scripts de abajo directamente.

---

## Cassandra

**Keyspace:** `safesight`  
**Estrategia de replicación:** `SimpleStrategy`, `replication_factor = 1`  
(Para producción con cluster multi-nodo: cambiar a `NetworkTopologyStrategy`)

### Crear keyspace

```cql
CREATE KEYSPACE IF NOT EXISTS safesight
WITH REPLICATION = {'class': 'SimpleStrategy', 'replication_factor': 1};

USE safesight;
```

---

### Tabla: `citizen_reports`

Tabla principal de reportes ciudadanos. Organizada para lecturas por alerta.

- **Partition key:** `(alert_id, time_bucket)` — agrupa por alerta y por día, evita hot partitions
- **Clustering key:** `reported_at ASC, id ASC` — permite rangos de tiempo y unicidad de fila

```cql
CREATE TABLE IF NOT EXISTS safesight.citizen_reports (
    alert_id    TEXT,
    time_bucket TEXT,
    reported_at TIMESTAMP,
    id          UUID,
    type        INT,
    citizen_id  TEXT,
    latitude    DOUBLE,
    longitude   DOUBLE,
    description TEXT,
    photo_url   TEXT,
    PRIMARY KEY ((alert_id, time_bucket), reported_at, id)
) WITH CLUSTERING ORDER BY (reported_at ASC, id ASC);
```

| Columna | Tipo | Descripción |
|---|---|---|
| `alert_id` | TEXT | ID de la alerta asociada |
| `time_bucket` | TEXT | Fecha del reporte en formato `YYYY-MM-DD` |
| `reported_at` | TIMESTAMP | Fecha y hora del reporte (UTC) |
| `id` | UUID | ID único del reporte |
| `type` | INT | Tipo: `0` = Awareness, `1` = Info |
| `citizen_id` | TEXT | ID del ciudadano informante (solo para tipo Info) |
| `latitude` | DOUBLE | Latitud de la ubicación del reporte |
| `longitude` | DOUBLE | Longitud de la ubicación del reporte |
| `description` | TEXT | Descripción (solo reportes tipo Info) |
| `photo_url` | TEXT | URL relativa de la foto (solo tipo Info, si tiene foto) |

---

### Tabla: `citizen_reports_by_time`

Tabla secundaria de denormalización para el `HeatmapSyncService`. Permite escanear todos los
reportes de un día sin conocer el `alert_id`. Solo contiene los campos necesarios para el heatmap.

- **Partition key:** `time_bucket`
- **Clustering key:** `reported_at ASC, id ASC`

```cql
CREATE TABLE IF NOT EXISTS safesight.citizen_reports_by_time (
    time_bucket TEXT,
    reported_at TIMESTAMP,
    alert_id    TEXT,
    id          UUID,
    type        INT,
    latitude    DOUBLE,
    longitude   DOUBLE,
    PRIMARY KEY (time_bucket, reported_at, id)
) WITH CLUSTERING ORDER BY (reported_at ASC, id ASC);
```

---

### Tabla: `info_reports_sync`

Tabla de staging para el `InfoReportSyncService`. Solo se escribe cuando el reporte es de tipo
Info. Contiene los campos completos para que el sync service los migre a MongoDB.

- **Partition key:** `time_bucket`
- **Clustering key:** `reported_at ASC, id ASC`

```cql
CREATE TABLE IF NOT EXISTS safesight.info_reports_sync (
    time_bucket TEXT,
    reported_at TIMESTAMP,
    id          UUID,
    alert_id    TEXT,
    citizen_id  TEXT,
    latitude    DOUBLE,
    longitude   DOUBLE,
    description TEXT,
    photo_url   TEXT,
    PRIMARY KEY (time_bucket, reported_at, id)
) WITH CLUSTERING ORDER BY (reported_at ASC, id ASC);
```

---

### Tabla: `sync_checkpoints`

Guarda el último timestamp procesado por cada background service de sincronización.
Permite lecturas incrementales sin reprocesar datos ya migrados.

```cql
CREATE TABLE IF NOT EXISTS safesight.sync_checkpoints (
    service_name      TEXT PRIMARY KEY,
    last_processed_at TIMESTAMP
);
```

| `service_name` | Descripción |
|---|---|
| `heatmap_sync` | Checkpoint del `HeatmapSyncService` |
| `info_report_sync` | Checkpoint del `InfoReportSyncService` |

---

## MongoDB

**Base de datos:** `safesight`

Las colecciones se crean automáticamente por EF Core al insertar el primer documento.
Los índices se crean explícitamente en el startup. A continuación el detalle de cada colección.

---

### Colección: `alerts`

Almacena las alertas de personas desaparecidas.

**Documento de ejemplo:**
```json
{
  "_id": "68415a2b1234abcd5678ef90",
  "MissingPerson": {
    "FirstName": "Juan",
    "LastName": "Pérez",
    "Age": 35,
    "PhysicalDescription": "Cabello castaño, 1.75m",
    "PhotoUrl": "/photos/uuid.jpg"
  },
  "Situation": "Descripción de la situación",
  "LastKnownLocation": {
    "Latitude": -34.603722,
    "Longitude": -58.381592
  },
  "DisappearanceDate": "2026-06-01T10:00:00Z",
  "EmitterId": 1,
  "EmittedAt": "2026-06-01T12:00:00Z",
  "Status": "Active",
  "ResolvedAt": null
}
```

**Índices:**
```js
db.alerts.createIndex({ "Status": 1 }, { name: "idx_alerts_status" })
db.alerts.createIndex({ "EmitterId": 1, "Status": 1 }, { name: "idx_alerts_emitter_status" })
```

---

### Colección: `heatmap_cells`

Almacena las celdas del mapa de calor pre-agregadas por el `HeatmapSyncService`.
Se actualiza cada 15 segundos con consistencia eventual desde Cassandra.

**Documento de ejemplo:**
```json
{
  "_id": "abc123|geohash",
  "AlertId": "abc123",
  "CenterLatitude": -34.603722,
  "CenterLongitude": -58.381592,
  "AwarenessCount": 45,
  "InfoCount": 8,
  "WeightedIntensity": 69.0,
  "LastUpdated": "2026-06-10T14:15:00Z"
}
```

> `WeightedIntensity = (InfoCount × 3) + (AwarenessCount × 1)`  
> El `_id` de celdas por alerta es `"{alertId}|{geohash}"`. El `_id` de celdas globales es `"global|{geohash}"`.

**Índices:**
```js
db.heatmap_cells.createIndex({ "AlertId": 1 }, { name: "idx_heatmap_alertid" })
```

---

### Colección: `info_reports`

Almacena los reportes de tipo Info (con descripción y foto) migrados desde Cassandra por el
`InfoReportSyncService`. Se actualiza cada 15 segundos con consistencia eventual.

**Documento de ejemplo:**
```json
{
  "_id": "uuid",
  "AlertId": "abc123",
  "CitizenId": "citizen-001",
  "Latitude": -34.603722,
  "Longitude": -58.381592,
  "Description": "Vi a una persona con campera roja...",
  "PhotoUrl": "/photos/uuid.jpg",
  "ReportedAt": "2026-06-10T14:30:00Z"
}
```

**Índices:**
```js
db.info_reports.createIndex({ "AlertId": 1 }, { name: "idx_inforeports_alertid" })
db.info_reports.createIndex({ "CitizenId": 1 }, { name: "idx_inforeports_citizenid" })
```

---

### Colección: `admin_activity`

Feed de eventos del sistema para el panel de administración. Se escribe en tiempo real
cuando se crean alertas, reportes o se cambia el estado de una alerta.

**Documento de ejemplo:**
```json
{
  "_id": "uuid",
  "Kind": "AlertCreated",
  "Title": "Nueva alerta emitida",
  "Description": "Se emitió una alerta para Juan Pérez (35 años).",
  "Timestamp": "2026-06-10T14:00:00Z",
  "AlertId": "abc123"
}
```

| `Kind` | Cuándo se genera |
|---|---|
| `AlertCreated` | Al crear una nueva alerta (`POST /api/alerts`) |
| `ReportSubmitted` | Al recibir un reporte ciudadano (`POST /api/reports/awareness` o `/info`) |
| `StatusChanged` | Al cambiar el estado de una alerta (`PATCH /api/alerts/:id/status`) |
| `System` | Eventos de sistema (reservado para uso futuro) |

**Índices:**
```js
db.admin_activity.createIndex({ "Timestamp": -1 }, { name: "idx_adminactivity_timestamp" })
```

---

### Colección: `device_tokens`

Almacena los tokens FCM de dispositivos registrados para notificaciones push.

**Índices:** ninguno adicional (el `_id` del documento es el propio token).

---

## Script completo de inicialización

### Cassandra — script único

```cql
CREATE KEYSPACE IF NOT EXISTS safesight
WITH REPLICATION = {'class': 'SimpleStrategy', 'replication_factor': 1};

USE safesight;

CREATE TABLE IF NOT EXISTS citizen_reports (
    alert_id    TEXT,
    time_bucket TEXT,
    reported_at TIMESTAMP,
    id          UUID,
    type        INT,
    citizen_id  TEXT,
    latitude    DOUBLE,
    longitude   DOUBLE,
    description TEXT,
    photo_url   TEXT,
    PRIMARY KEY ((alert_id, time_bucket), reported_at, id)
) WITH CLUSTERING ORDER BY (reported_at ASC, id ASC);

CREATE TABLE IF NOT EXISTS citizen_reports_by_time (
    time_bucket TEXT,
    reported_at TIMESTAMP,
    alert_id    TEXT,
    id          UUID,
    type        INT,
    latitude    DOUBLE,
    longitude   DOUBLE,
    PRIMARY KEY (time_bucket, reported_at, id)
) WITH CLUSTERING ORDER BY (reported_at ASC, id ASC);

CREATE TABLE IF NOT EXISTS info_reports_sync (
    time_bucket TEXT,
    reported_at TIMESTAMP,
    id          UUID,
    alert_id    TEXT,
    citizen_id  TEXT,
    latitude    DOUBLE,
    longitude   DOUBLE,
    description TEXT,
    photo_url   TEXT,
    PRIMARY KEY (time_bucket, reported_at, id)
) WITH CLUSTERING ORDER BY (reported_at ASC, id ASC);

CREATE TABLE IF NOT EXISTS sync_checkpoints (
    service_name      TEXT PRIMARY KEY,
    last_processed_at TIMESTAMP
);
```

### MongoDB — script único (mongo shell)

```js
use safesight

db.alerts.createIndex({ "Status": 1 }, { name: "idx_alerts_status" })
db.alerts.createIndex({ "EmitterId": 1, "Status": 1 }, { name: "idx_alerts_emitter_status" })

db.heatmap_cells.createIndex({ "AlertId": 1 }, { name: "idx_heatmap_alertid" })

db.info_reports.createIndex({ "AlertId": 1 }, { name: "idx_inforeports_alertid" })
db.info_reports.createIndex({ "CitizenId": 1 }, { name: "idx_inforeports_citizenid" })

db.admin_activity.createIndex({ "Timestamp": -1 }, { name: "idx_adminactivity_timestamp" })
```
