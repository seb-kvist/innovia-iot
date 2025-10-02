# Innovia Hub – IoT API (Monorepo, .NET 8)

Innovia Hub is a comprehensive IoT platform for smart buildings and office hubs, enabling organizations to seamlessly connect, manage, and monitor IoT devices and sensors at scale. The system provides robust APIs and real-time data integration for efficient facility management and automation.

This monorepo contains all core services: Device Registry, Ingest Gateway, Realtime Hub, Portal Adapter, and Rules Engine, as well as an Edge Simulator for device and data testing.

## Arkitektur
- **DeviceRegistry.Api** – Web API för tenants/sites/rooms/devices/sensors och nyckelhantering (EF Core, PostgreSQL).
- **Ingest.Gateway** – HTTP-ingest + MQTT-konsument som normaliserar och sparar mätdata (EF Core).
- **Realtime.Hub** – SignalR-hub som pushar senaste värden per tenant/room/device.
- **Portal.Adapter** – Förenklade endpoints för externa portaler + (plats för) webhooks.
- **Rules.Engine** – Worker Service som kör regler och skapar alerts.
- **Edge.Simulator** – Konsolapp som publicerar MQTT-mätningar.

Databas: **PostgreSQL (TimescaleDB-kompatibel)**. Cache/queue: **Redis**. MQTT: **Eclipse Mosquitto**.

## Snabbstart
1. Installera **Docker** & **Docker Compose** och **.NET 8 SDK**.
2. `docker compose -f deploy/docker-compose.yml up -d` (startar db/redis/mosquitto)
3. I ett nytt terminalfönster, kör tjänsterna lokalt (exempel):
   - `dotnet restore`
   - `dotnet build`
   - Starta **DeviceRegistry.Api**, **Ingest.Gateway**, **Realtime.Hub**, **Portal.Adapter** (varsin terminal)
4. Kör **Edge.Simulator** för att skicka data via MQTT.
5. Koppla din egen portal mot **Realtime.Hub** (SignalR) och **Portal.Adapter** (REST).

Se `docs/architecture.md` och `docs/api-specs.md` för detaljer.

## Guider: Köra och använda systemet

### 1. Starta systemet
- Säkerställ att **Docker Desktop** körs.
- Kör `docker compose -f deploy/docker-compose.yml up -d` för att starta databasen (Postgres), Redis och Mosquitto.
- Verifiera att containrarna är igång: `docker ps`.

### 2. Starta tjänsterna
- Öppna separata terminaler för varje tjänst:
  ```bash
  cd src/DeviceRegistry.Api && dotnet run
  cd src/Ingest.Gateway && dotnet run
  cd src/Realtime.Hub && dotnet run
  cd src/Portal.Adapter && dotnet run
  ```
- Swagger finns på respektive port, t.ex. http://localhost:5101/swagger.

### 3. Skapa en tenant och en device
- Skapa tenant via DeviceRegistry:
  ```bash
  curl -X POST http://localhost:5101/api/tenants \
    -H "Content-Type: application/json" \
    -d '{ "name": "Innovia Hub", "slug": "innovia" }'
  ```
- Skapa en device under tenant:
  ```bash
  curl -X POST http://localhost:5101/api/tenants/<TENANT_ID>/devices \
    -H "Content-Type: application/json" \
    -d '{ "model":"Acme CO2-Temp", "serial":"dev-101", "status":"active" }'
  ```

### 4. Skicka in mätdata
- Via Ingest HTTP:
  ```bash
  curl -X POST http://localhost:5102/ingest/http/innovia \
    -H "Content-Type: application/json" \
    -d '{
      "deviceId": "dev-101",
      "apiKey": "dev-101-key",
      "timestamp": "2025-10-01T12:00:00Z",
      "metrics": [
        { "type": "temperature", "value": 22.5, "unit": "C" },
        { "type": "co2", "value": 1000, "unit": "ppm" }
      ]
    }'
  ```

### 5. Läs data via Portal.Adapter
- Hämta mätserier:
  ```bash
  curl "http://localhost:5104/portal/innovia/devices/<DEVICE_ID>/series?type=co2&from=2025-09-01T00:00:00Z&to=2025-10-01T23:59:59Z"
  ```
- Du får tillbaka JSON med tidsstämplade datapunkter.

### 6. Realtidsdata
- Anslut till SignalR-hubben (Realtime.Hub) på `http://localhost:5103/hub/telemetry`.
- Exempel i JavaScript:
  ```js
  const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5103/hub/telemetry")
    .build();
  await connection.start();
  await connection.invoke("JoinTenant", "innovia");
  connection.on("measurementReceived", data => console.log("Realtime:", data));
  ```

### 7. Tips
- Använd Swagger för att testa endpoints.
- Kolla databasen med `docker exec -it <db-container> psql -U postgres -d innovia -c "\dt"`.
- Kombinera DeviceRegistry + Ingest + Portal.Adapter för end-to-end-flöde.

## Ports (förslag)
- DeviceRegistry.Api: http://localhost:5101
- Ingest.Gateway:    http://localhost:5102
- Realtime.Hub:      http://localhost:5103
- Portal.Adapter:    http://localhost:5104
- Mosquitto (MQTT):  localhost:1883
- PostgreSQL:        localhost:5432
- Redis:             localhost:6379

## EF Core migreringar
Skapa & kör migreringar i **DeviceRegistry.Api** och **Ingest.Gateway**. Exempel:
```bash
cd src/DeviceRegistry.Api
dotnet ef migrations add Init --project DeviceRegistry.Api.csproj
dotnet ef database update
```
(Se till att ha verktygen: `dotnet tool install --global dotnet-ef`)

## Tips för uppgiften
- Implementera HMAC för HTTP-ingest.
- Lägg till ny sensortyp & visualisering.
- Lägg på RBAC/JWT och tenant-isolering.
- Bygg webhook-sändare i Portal.Adapter.
