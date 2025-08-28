# Changelog

- Preserved services and calculation logic from yesterday (correct dataset field mappings).
- Integrated today's infrastructure updates: UI (with proxy + feedback), Gateway (5000), Orchestrator (JSON + /result/{runId}), RabbitMQ with docker-compose networking.
- Added Dockerfile.ui and nginx.conf for UI proxying.
- Dataset DataSetClean.csv included in /data and mounted at /app/data/DataSetClean.csv.
