# Catalog API

Brief: Catalog microservice providing product data via REST endpoints.

Prerequisites:
- .NET 5 or later
- Docker Desktop (for running the full stack)

Run (Docker Compose):
1. From repository root run:

```bash
cd src
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d
```

Local dev (no Docker):
```bash
cd src/Services/Catalog/Catalog.API
dotnet run
```

Swagger: http://host.docker.internal:8000/swagger/index.html

Config: see `src/docker-compose.override.yml` for example environment variables and connection strings.

Tests: `dotnet test` against corresponding test project in `tests/Catalog`.
