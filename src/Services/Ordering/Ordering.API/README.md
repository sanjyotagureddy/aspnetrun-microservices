# Ordering API

Brief: Ordering microservice implementing DDD and CQRS patterns; consumes BasketCheckout events.

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
cd src/Services/Ordering/Ordering.API
dotnet run
```

Swagger: http://host.docker.internal:8004/swagger/index.html

Config: ordering DB connection is configured in `src/docker-compose.override.yml`.

Tests: `dotnet test` against `tests/Ordering`.
