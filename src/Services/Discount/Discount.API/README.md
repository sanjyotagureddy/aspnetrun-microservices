# Discount API

Brief: Discount microservice exposing data via REST and persisting to PostgreSQL.

Prerequisites:
- .NET 5 or later
- Docker Desktop

Run (Docker Compose):
```bash
cd src
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d
```

Local dev:
```bash
cd src/Services/Discount/Discount.API
dotnet run
```

Swagger: http://host.docker.internal:8002/swagger/index.html

Config: DB connection shown in `src/docker-compose.override.yml` (replace defaults in production).

Tests: `dotnet test` against `tests/Discount`.
