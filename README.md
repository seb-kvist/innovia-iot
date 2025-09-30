# Innovia Hub – IoT API (Monorepo, .NET 8)

Ett övningssystem för ett kontorshotell (Innovia Hub) som vill koppla upp sensorer via en inköpt IoT-tjänst.
Repo innehåller flera tjänster (Device Registry, Ingest Gateway, Realtime Hub, Portal Adapter, Rules Engine) samt en Edge-simulator.

> **Mål**: Studenterna ska kunna klona, starta via Docker Compose, läsa kodbasen, konfigurera tenant/rum/device, simulera data och integrera sin egen portal via REST/SignalR/Webhooks.

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
