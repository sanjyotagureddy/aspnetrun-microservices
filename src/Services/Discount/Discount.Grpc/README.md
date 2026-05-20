# Discount gRPC Service

Brief: gRPC service used for synchronous discount lookups by other microservices.

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
cd src/Services/Discount/Discount.Grpc
dotnet run
```

GRPC endpoint: available at http://host.docker.internal:8003

Config and DB: see `src/docker-compose.override.yml`.

Tests: `dotnet test` against `tests/Discount.Grpc`.
