# HitRate Calculator (Microservices + UI)

- .NET 8, MassTransit + RabbitMQ, YARP gateway, static UI (nginx).
- Default dataset path: `/data/DataSetClean.csv` (place your CSV in `data/`).
- UI prompts for variables and calls the gateway `POST /runs`.

## Run
```bash
docker compose up --build
# UI: http://localhost:3000
# Gateway: http://localhost:8000
# Orchestrator: http://localhost:8080
# RabbitMQ: http://localhost:15672 (guest/guest)
```
