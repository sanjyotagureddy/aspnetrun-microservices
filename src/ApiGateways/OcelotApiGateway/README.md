# Ocelot API Gateway

Brief: Ocelot-based API Gateway routing requests to backend microservices.

Prerequisites:
- .NET 5 or later
- Docker Desktop

Run (Docker Compose):
```bash
cd src
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d
```

Gateway URL: http://host.docker.internal:8010

Config: routes and downstream services are configured in `ocelot.json` and `ocelot.Local.json`.
