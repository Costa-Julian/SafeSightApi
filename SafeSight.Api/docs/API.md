# SafeSight API — Documentación

**Base URL:** `http://10.100.40.111:5121`  
**Formato:** JSON (excepto donde se indica `multipart/form-data`)

> La API corre en la PC con IP `10.100.40.111`. Cualquier dispositivo en la misma red puede consumirla usando esa dirección. Si la IP cambia (DHCP), actualizá esta URL.

---

## Estructura de respuesta

Todos los endpoints devuelven el mismo wrapper:

```json
{
  "success": true,
  "data": { ... },
  "error": null
}
```

En caso de error:
```json
{
  "success": false,
  "data": null,
  "error": "Descripción del error."
}
```

### Respuesta paginada

Cuando `data` es una lista paginada:

```json
{
  "success": true,
  "data": {
    "items": [ ... ],
    "page": 1,
    "pageSize": 20,
    "totalItems": 45,
    "totalPages": 3
  }
}
```

---

## Enums

| Tipo | Valores |
|---|---|
| `AlertStatus` | `"Active"`, `"Resolved"`, `"Cancelled"` |
| `ReportType` | `0` = Awareness, `1` = Info |

---

## Alertas — `/api/alerts`

### GET `/api/alerts`
Lista todas las alertas activas, paginadas.

**Query params:**
| Param | Tipo | Default | Descripción |
|---|---|---|---|
| `page` | int | 1 | Número de página |
| `pageSize` | int | 20 | Resultados por página (máx 100) |

**Respuesta `200`:** `PagedResponse<AlertResponse>`

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "abc123",
        "firstName": "Juan",
        "lastName": "Pérez",
        "age": 35,
        "physicalDescription": "Cabello castaño, 1.75m",
        "photoUrl": "/photos/uuid.jpg",
        "situation": "Descripción de la situación",
        "lastKnownLatitude": -34.603722,
        "lastKnownLongitude": -58.381592,
        "disappearanceDate": "2026-06-01T10:00:00Z",
        "emitterId": 1,
        "emittedAt": "2026-06-01T12:00:00Z",
        "status": "Active"
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalItems": 5,
    "totalPages": 1
  }
}
```

---

### GET `/api/alerts/{id}`
Obtiene una alerta por su ID.

**Path params:** `id` (string)

**Respuesta `200`:** `AlertResponse` (mismo objeto del listado)  
**Respuesta `404`:** `{ "success": false, "error": "Alerta no encontrada." }`

---

### GET `/api/alerts/{id}/metrics`
Métricas de participación ciudadana para una alerta.

**Path params:** `id` (string)

**Respuesta `200`:**
```json
{
  "success": true,
  "data": {
    "alertId": "abc123",
    "totalAwareness": 120,
    "totalInfoReports": 15,
    "geographicReachKm": 4.7
  }
}
```

**Respuesta `404`:** Alerta no encontrada.

---

### GET `/api/alerts/by-emitter/{emitterId}`
Lista las alertas creadas por un emisor específico.

**Path params:** `emitterId` (int)  
**Query params:** `page`, `pageSize` (igual que GET `/api/alerts`)

**Respuesta `200`:** `PagedResponse<AlertResponse>`

---

### POST `/api/alerts`
Crea una nueva alerta. Requiere `multipart/form-data`.

**Body (`multipart/form-data`):**
| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| `firstName` | string | Sí | Nombre de la persona |
| `lastName` | string | Sí | Apellido |
| `age` | int | Sí | Edad |
| `physicalDescription` | string | Sí | Descripción física |
| `situation` | string | Sí | Descripción de la situación |
| `latitude` | double | Sí | Latitud del último lugar conocido |
| `longitude` | double | Sí | Longitud del último lugar conocido |
| `disappearanceDate` | datetime | Sí | Fecha de desaparición (ISO 8601) |
| `emitterId` | int | Sí | ID del emisor (1=ciudadano, 2=entidad) |
| `photo` | file | No | Foto de la persona (jpg/png, máx 5 MB) |

**Respuesta `201`:** `AlertResponse` con el objeto creado.

---

### PATCH `/api/alerts/{id}/status`
Actualiza el estado de una alerta.

**Path params:** `id` (string)

**Body (JSON):**
```json
{
  "status": "Resolved"
}
```
Valores válidos: `"Active"`, `"Resolved"`, `"Cancelled"`

**Respuesta `200`:** `{ "success": true, "data": true }`  
**Respuesta `404`:** Alerta no encontrada.

---

## Reportes ciudadanos — `/api/reports`

### POST `/api/reports/awareness`
El ciudadano reporta que está enterado de la alerta, enviando solo su ubicación.

**Body (JSON):**
```json
{
  "alertId": "abc123",
  "latitude": -34.603722,
  "longitude": -58.381592,
  "reportedAt": "2026-06-07T14:30:00Z"
}
```

| Campo | Tipo | Requerido |
|---|---|---|
| `alertId` | string | Sí |
| `latitude` | double | Sí (-90 a 90) |
| `longitude` | double | Sí (-180 a 180) |
| `reportedAt` | datetime | Sí |

**Respuesta `200`:**
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "alertId": "abc123",
    "type": 0,
    "latitude": -34.603722,
    "longitude": -58.381592,
    "reportedAt": "2026-06-07T14:30:00Z"
  }
}
```

---

### POST `/api/reports/info`
El ciudadano reporta información con descripción y foto opcional. Requiere `multipart/form-data`.

**Body (`multipart/form-data`):**
| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| `alertId` | string | Sí | ID de la alerta |
| `citizenId` | string | Sí | ID del ciudadano |
| `latitude` | double | Sí | Latitud (-90 a 90) |
| `longitude` | double | Sí | Longitud (-180 a 180) |
| `description` | string | Sí | Descripción (máx 1000 caracteres) |
| `reportedAt` | datetime | Sí | Fecha del reporte (ISO 8601) |
| `photo` | file | No | Foto (jpg/png, máx 5 MB) |

**Respuesta `200`:**
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "alertId": "abc123",
    "citizenId": "citizen-001",
    "type": 1,
    "latitude": -34.603722,
    "longitude": -58.381592,
    "description": "Vi a una persona...",
    "photoUrl": "/photos/uuid.jpg",
    "reportedAt": "2026-06-07T14:30:00Z"
  }
}
```

---

### GET `/api/reports/info/by-alert/{alertId}`
Lista todos los reportes de tipo Info (con descripción y foto) para una alerta, ordenados del más nuevo al más viejo. Lee desde MongoDB.

> Los reportes aparecen con hasta ~15 segundos de retraso desde que se crean (consistencia eventual).

**Path params:** `alertId` (string)  
**Query params:** `page`, `pageSize` (máx 100, default 50)

**Respuesta `200`:** `PagedResponse<InfoReportDocument>`

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "uuid",
        "alertId": "abc123",
        "citizenId": "citizen-001",
        "latitude": -34.603722,
        "longitude": -58.381592,
        "description": "Vi a una persona con campera roja...",
        "photoUrl": "/photos/uuid.jpg",
        "reportedAt": "2026-06-07T14:30:00Z"
      }
    ],
    "page": 1,
    "pageSize": 50,
    "totalItems": 8,
    "totalPages": 1
  }
}
```

---

### GET `/api/reports/by-alert/{alertId}`
Lista todos los reportes (Awareness + Info) para una alerta. Lee desde Cassandra.

**Path params:** `alertId` (string)  
**Query params:** `page`, `pageSize` (máx 100, default 50)

**Respuesta `200`:** `PagedResponse<CitizenReport>` (incluye ambos tipos)

---

## Estadísticas — `/api/stats`

### GET `/api/stats/overview`
Resumen general del sistema.

**Respuesta `200`:**
```json
{
  "success": true,
  "data": {
    "totalAlerts": 10,
    "activeAlerts": 6,
    "resolvedAlerts": 3,
    "cancelledAlerts": 1,
    "totalReports": 540,
    "totalAwarenessReports": 480,
    "totalInfoReports": 60
  }
}
```

---

### GET `/api/stats/heatmap`
Datos del mapa de calor para renderizar en el mapa.

**Query params:**
| Param | Tipo | Descripción |
|---|---|---|
| `alertId` | string | Opcional. Si se omite, devuelve el heatmap global. |

**Respuesta `200`:**
```json
{
  "success": true,
  "data": {
    "alertId": "abc123",
    "cells": [
      {
        "centerLatitude": -34.603722,
        "centerLongitude": -58.381592,
        "awarenessCount": 45,
        "infoCount": 8,
        "weightedIntensity": 69.0,
        "lastUpdated": "2026-06-07T14:15:00Z"
      }
    ]
  }
}
```

> `weightedIntensity` = `(infoCount × 3) + (awarenessCount × 1)`

---

## Notificaciones — `/api/notifications`

### POST `/api/notifications/register-token`
Registra o actualiza un token FCM de un dispositivo para recibir notificaciones push.

**Body (JSON):**
```json
{
  "token": "fcm-token-string",
  "role": "citizen"
}
```

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| `token` | string | Sí | Token FCM del dispositivo |
| `role` | string | Sí | Rol del dispositivo (ej: `"citizen"`, `"entity"`) |

**Respuesta `200`:** `{ "success": true, "data": true }`  
**Respuesta `400`:** Token inválido.

---

## Archivos estáticos

Las fotos se sirven directamente como archivos estáticos:

```
GET http://10.100.40.111:5121/photos/<nombre-archivo>.jpg
```

La URL de la foto viene en los campos `photoUrl` de las respuestas de alertas y reportes.
