# SafeSight API — Documentación de Endpoints

Todas las respuestas usan el wrapper `ApiResult<T>`:

```json
{ "success": true, "data": { ... }, "error": null }
{ "success": false, "data": null, "error": "Mensaje de error." }
```

Base URL: `http://localhost:5121`

---

## Alertas

### GET /api/alerts

Lista paginada de alertas activas.

**Query params:** `page` (default 1), `pageSize` (default 20, máx 100)

**Respuesta 200:**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "...",
        "firstName": "Valentina",
        "lastName": "Ríos",
        "age": 9,
        "physicalDescription": "...",
        "photoUrl": "https://i.pravatar.cc/300?img=47",
        "situation": "...",
        "lastKnownLatitude": -34.6037,
        "lastKnownLongitude": -58.3816,
        "disappearanceDate": "2026-05-25T00:00:00Z",
        "emitterId": 1,
        "emittedAt": "2026-05-25T01:00:00Z",
        "status": "Active"
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalItems": 4,
    "totalPages": 1
  }
}
```

---

### GET /api/alerts/{id}

Detalle de una alerta por ID.

**Respuesta 200:** `ApiResult<AlertResponse>`  
**Respuesta 404:** Alerta no encontrada.

---

### GET /api/alerts/{id}/metrics

Métricas calculadas desde Cassandra para una alerta.

**Respuesta 200:**
```json
{
  "success": true,
  "data": {
    "alertId": "...",
    "totalAwareness": 52,
    "totalInfoReports": 18,
    "geographicReachKm": 3.47
  }
}
```

---

### GET /api/alerts/by-emitter/{emitterId}

Alertas de un emisor específico (1=ciudadano, 2=entidad).

**Query params:** `page`, `pageSize`  
**Respuesta 200:** `ApiResult<PagedResponse<AlertResponse>>`

---

### POST /api/alerts

Crear una nueva alerta. Content-Type: `multipart/form-data`.

**Body fields:**

| Campo | Tipo | Requerido |
|-------|------|-----------|
| firstName | string | Sí |
| lastName | string | Sí |
| age | int | Sí |
| physicalDescription | string | Sí |
| situation | string | Sí |
| latitude | double | Sí |
| longitude | double | Sí |
| disappearanceDate | datetime | Sí |
| emitterId | int (1 o 2) | Sí |
| photo | file (jpg/png) | No |

**Respuesta 201:** `ApiResult<AlertResponse>` con el objeto creado.

---

### PATCH /api/alerts/{id}/status

Cambiar el estado de una alerta.

**Body:**
```json
{ "status": "Resolved" }
```

Valores posibles: `Active`, `Resolved`, `Cancelled`

**Respuesta 200:** `ApiResult<bool>`  
**Respuesta 404:** Alerta no encontrada.

---

## Reportes Ciudadanos

### POST /api/reports/awareness

Registrar un "Enterado" (punto de alcance de alerta).

**Body JSON:**
```json
{
  "alertId": "...",
  "latitude": -34.61,
  "longitude": -58.39,
  "reportedAt": "2026-05-27T14:30:00Z"
}
```

**Respuesta 200:** `ApiResult<CitizenReport>`

---

### POST /api/reports/info

Registrar "Tengo información" (punto de interés con evidencia). Content-Type: `multipart/form-data`.

**Body fields:**

| Campo | Tipo | Requerido |
|-------|------|-----------|
| alertId | string | Sí |
| latitude | double | Sí |
| longitude | double | Sí |
| description | string | Sí |
| reportedAt | datetime | Sí |
| photo | file (jpg/png) | No |

**Respuesta 200:** `ApiResult<CitizenReport>`

---

### GET /api/reports/by-alert/{alertId}

Reportes paginados de una alerta (últimos 30 días).

**Query params:** `page` (default 1), `pageSize` (default 50, máx 100)

**Respuesta 200:** `ApiResult<PagedResponse<CitizenReport>>`

---

## Estadísticas

### GET /api/stats/overview

Estadísticas globales para administración.

**Respuesta 200:**
```json
{
  "success": true,
  "data": {
    "totalAlerts": 4,
    "activeAlerts": 4,
    "resolvedAlerts": 0,
    "cancelledAlerts": 0,
    "totalReports": 237,
    "totalAwarenessReports": 196,
    "totalInfoReports": 41
  }
}
```

---

### GET /api/stats/heatmap

Celdas de heatmap pre-agregadas. Siempre se leen de la colección `heatmap_cells` de MongoDB (nunca se calculan al vuelo).

**Query params:** `alertId` (opcional — si se omite, devuelve el heatmap global)

**Respuesta 200:**
```json
{
  "success": true,
  "data": {
    "alertId": "...",
    "cells": [
      {
        "centerLatitude": -34.60,
        "centerLongitude": -58.38,
        "awarenessCount": 8,
        "infoCount": 3,
        "weightedIntensity": 17.0,
        "lastUpdated": "2026-05-27T14:30:15Z"
      }
    ]
  }
}
```

`weightedIntensity = infoCount × 3 + awarenessCount × 1`

---

## Notificaciones

### POST /api/notifications/register-token

Registrar un token FCM para notificaciones push.

**Body JSON:**
```json
{ "token": "fcm_token_...", "role": "citizen" }
```

**Respuesta 200:** `ApiResult<bool>` con `data: true`

---

## Códigos de estado

| Código | Significado |
|--------|-------------|
| 200 | OK |
| 201 | Creado (POST /api/alerts) |
| 400 | Validación fallida |
| 404 | Recurso no encontrado |
| 500 | Error interno del servidor |
