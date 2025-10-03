# Rules.Engine dokumentation
# Rules.Engine (Work in Progress)

The **Rules.Engine** service is part of the Innovia IoT platform.  
Its purpose is to evaluate incoming IoT measurements against user-defined rules and trigger alerts when conditions are met.

⚠️ **Important:** This service is currently a **work in progress**. It provides a basic foundation but is not yet production-ready. Expect breaking changes and incomplete features.

---

## Purpose

Rules.Engine allows tenants to define simple conditions (rules) for their devices.  
For example:

- *"If temperature > 28°C, raise an alert."*
- *"If CO₂ level exceeds 1200 ppm, notify in real-time."*

When a rule matches, the engine stores an **alert** in the database and (optionally) pushes it to the **Realtime.Hub** via SignalR.

---

## Current Features

- PostgreSQL-backed storage for rules and alerts.
- Minimal API endpoints:
  - `POST /rules` → create a new rule.
  - `GET /rules` → list existing rules.
  - `GET /alerts` → query recent alerts.
- Background worker that:
  - Polls recent measurements from **Ingest** database.
  - Evaluates rules for each tenant/device.
  - Creates alerts when thresholds are exceeded.
- SignalR client integration prepared (publishes alerts to Realtime.Hub).

---

## Usage (MVP)

1. Start **Rules.Engine** (requires `innovia_rules` and `innovia_ingest` databases).
   ```bash
   cd src/Rules.Engine
   dotnet run
   ```

2. Create a rule:
   ```bash
   curl -X POST http://localhost:5105/rules \
     -H "Content-Type: application/json" \
     -d '{
       "tenantId": "your-tenant-guid",
       "deviceId": "your-device-guid",
       "type": "temperature",
       "op": ">",
       "threshold": 28.0,
       "cooldownSeconds": 300,
       "enabled": true
     }'
   ```

3. List rules:
   ```bash
   curl http://localhost:5105/rules
   ```

4. View alerts:
   ```bash
   curl http://localhost:5105/alerts
   ```

---

## Roadmap / TODOs

- [ ] Improve rule evaluation (support aggregations, time windows).
- [ ] Add authentication and multi-tenant isolation.
- [ ] Full admin API for rule management.
- [ ] Richer alert payloads (severity levels, metadata).
- [ ] Better integration with **Realtime.Hub** (dedicated `alertRaised` event).
- [ ] Replace `EnsureCreated()` with EF migrations for production.
- [ ] Unit and integration testing.

---

## Status

This service should be seen as an **MVP / proof-of-concept**.  
It is included in the repository to give developers a starting point for experimentation and extension.

Contributions and improvements are expected in future iterations.