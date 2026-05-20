# Shopping.Aggregator

Brief: Aggregator/BFF that combines multiple microservice APIs for simpler UI consumption.

Prerequisites:
- .NET 5 or later
- Docker Desktop

Run (Docker Compose):
```bash
cd src
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d
```

Swagger: http://host.docker.internal:8005/swagger/index.html

Config: API URLs are configured in `src/docker-compose.override.yml`.

Tests: `dotnet test` against `tests/Shopping.Aggregator.Test`.
