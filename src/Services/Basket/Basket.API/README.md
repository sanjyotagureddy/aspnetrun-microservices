# Basket API

Brief: Basket microservice managing user shopping baskets, uses Redis and publishes checkout events.

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
cd src/Services/Basket/Basket.API
dotnet run
```

Swagger: http://host.docker.internal:8001/swagger/index.html

Config: cache connection and Grpc discount URL are set in `src/docker-compose.override.yml`.

Tests: `dotnet test` against `tests/Basket`.
