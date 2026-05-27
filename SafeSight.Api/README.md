# SafeSight API

Backend REST de SafeSight, plataforma colaborativa de búsqueda de personas desaparecidas que complementa el sistema oficial argentino "Alerta Sofía". Proyecto académico de Ingeniería de Datos.

---

## Requisitos previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [MongoDB](https://www.mongodb.com/try/download/community) corriendo en `localhost:27017`
- [Apache Cassandra](https://cassandra.apache.org/download/) corriendo en `localhost:9042`

---

## Configuración de conexiones

Editar `SafeSight.Api/appsettings.json`:

```json
"DatabaseSettings": {
  "MongoDb": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "safesight"
  },
  "Cassandra": {
    "ContactPoints": [ "localhost" ],
    "Port": 9042,
    "Keyspace": "safesight"
  }
}
```

Cuando las bases se muevan a Docker, solo cambiá estos valores. Ningún archivo de código necesita modificación.

---

## Cómo correr

```bash
cd SafeSight.Api
dotnet run
```

La API arranca en `http://localhost:5121`.

Al iniciar, automáticamente:
1. Crea el keyspace y tablas en Cassandra
2. Crea las colecciones e índices en MongoDB
3. Carga datos de prueba (4 alertas + ~240 reportes ciudadanos) si las bases están vacías
4. El `HeatmapSyncService` comienza a sincronizar datos cada 15 segundos

---

## Swagger UI

Acceder a `http://localhost:5121/swagger` para explorar y probar todos los endpoints.

---

## Arquitectura de datos

### Cassandra — capa de ingesta (write-optimized)

Cassandra es la fuente de la verdad de los **eventos crudos**. Absorbe el aluvión de reportes ciudadanos sin degradar performance, optimizado para picos masivos de escritura concurrente.

**Tablas:**
- `citizen_reports`: partition key `(alert_id, time_bucket)` + clustering `reported_at, id` — permite lecturas eficientes por alerta y día
- `citizen_reports_by_time`: partition key `time_bucket` — permite al sync service escanear todos los reportes de un período sin conocer los alert_ids
- `sync_checkpoints`: guarda el último timestamp procesado por el HeatmapSyncService

El uso de `time_bucket` (fecha YYYY-MM-DD) como parte de la partition key evita hot partitions en alertas con muchos reportes.

### MongoDB — capa de lectura y análisis (read-optimized)

MongoDB sirve las consultas complejas y los datos procesados.

**Colecciones:**
- `alerts`: datos maestros de alertas y personas desaparecidas
- `heatmap_cells`: celdas de heatmap pre-agregadas (Data Mart analítico)

### HeatmapSyncService — consistencia eventual

Un `BackgroundService` de .NET corre dentro de la API y, cada 15 segundos (configurable):

1. Lee de Cassandra los reportes nuevos desde el último checkpoint
2. Agrupa por geohash (celdas de ~1 km²)
3. Calcula intensidad ponderada: `InfoCount × 3 + AwarenessCount × 1`
4. Hace upsert de las celdas en MongoDB
5. Actualiza el checkpoint

El heatmap en MongoDB está **unos segundos atrasado** respecto a los eventos en Cassandra. Esta es una decisión de diseño deliberada (consistencia eventual), no un defecto. El cliente Android consume celdas ya calculadas, nunca puntos crudos.

**`GET /api/stats/heatmap` siempre lee la colección pre-agregada.** Nunca calcula al vuelo. Este es el concepto de Data Mart de la materia: estructura optimizada para consulta analítica.

---

## Variables de configuración

| Sección | Clave | Default | Descripción |
|---------|-------|---------|-------------|
| SyncSettings | HeatmapSyncIntervalSeconds | 15 | Intervalo del sync en segundos |
| HeatmapSettings | InfoPointWeight | 3 | Peso de reportes tipo Info |
| HeatmapSettings | AwarenessPointWeight | 1 | Peso de reportes tipo Awareness |
| SeedSettings | EnableSeedOnStartup | true | Deshabilitar para no cargar datos de prueba |
| PhotoSettings | MaxFileSizeMb | 5 | Tamaño máximo de foto subida |
