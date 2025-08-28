# Run Instructions

```bash
docker compose up --build
```

## Ports
- UI: http://localhost:3000 (Public)
- Gateway: http://localhost:5000 (Public)
- Orchestrator: http://localhost:5001 (Private)
- RabbitMQ: 5672 private, 15672 public (if enabled)

## Flow
1. Open UI at port 3000.
2. Fill parameters and click **Start Run**.
3. UI shows feedback ("Submitting..." → "Run started..." → "Processing...").
4. When calculation completes, UI fetches result and displays hit rate.
