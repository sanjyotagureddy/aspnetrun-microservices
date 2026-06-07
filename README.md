# aspnetrun-microservices

Microservices sample built with ASP.NET Core, .NET Aspire, PostgreSQL, Kafka, gRPC, YARP-named gateway scaffolding, and shared cross-cutting libraries.

The repository is currently centered on the Product service. Cart, Order, Discount, and Gateway projects are present in the Aspire host, but several still contain template endpoints while their domain behavior is being built out.

## Current Architecture

- **Aspire AppHost** orchestrates local resources and service projects.
- **ServiceDefaults** configures OpenTelemetry, service discovery, HTTP resilience, and health endpoints.
- **Products API** implements product CRUD/search features using Minimal API endpoint discovery, MediatR, FluentValidation, Dapper, PostgreSQL, Serilog, rate limiting, response compression, output caching, centralized exception handling, and publishes a `products.created` message through the shared messaging abstraction after a product is created.
- **Cart API** is registered with Aspire and currently contains template API scaffolding.
- **Order API** is registered with Aspire, uses shared observability context middleware, and currently contains template API scaffolding.
- **Discount gRPC** is registered with Aspire and currently exposes the template greeter service.
- **Gateway.Yarp** is registered with Aspire and currently contains template API scaffolding. YARP reverse proxy routes are not implemented yet.
- **Shared Kernel** contains common abstractions for entities, value objects, domain events, integration events, messaging, validation, results, exceptions, endpoint registration, and observability context.
- **Shared Logging** contains a custom logging pipeline, formatters, enrichers, filters, and console/file/Elasticsearch sink support.
- **Shared Messaging** contains broker-agnostic messaging contracts, metadata/envelope models, DI registration, observability primitives, and the first Kafka provider implementation.

## Repository Layout

```text
src/
  aspire/
    aspnetrun-microservices.AppHost/
    aspnetrun-microservices.ServiceDefaults/
  Gateway/
    Gateway.Yarp/
  Services/
    Cart/
    Discount/
    Order/
    Product/
  Shared/
    Common.SharedKernel/
    Common.SharedKernel.Logging/
    Common.SharedKernel.Messaging/
    Common.SharedKernel.Messaging.RabbitMq/
tests/
  IntegrationTests/
  UnitTests/
```

## Prerequisites

- .NET SDK 10
- Docker Desktop or another supported container runtime for Aspire-hosted resources
- An IDE or editor that supports `.slnx`, or the `dotnet` CLI

The expected SDK is pinned in `global.json`.

## Run Locally

From the repository root:

```powershell
dotnet run --project src\aspire\aspnetrun-microservices.AppHost\aspnetrun-microservices.AppHost.csproj
```

The Aspire dashboard will show the service URLs and provisioned resources. The AppHost currently starts:

- PostgreSQL resource `productsdb`
- PostgreSQL database `products`
- Kafka resource `message-broker`
- Products API
- Cart API
- Discount gRPC
- Order API
- Gateway.Yarp

The Products API receives the Aspire Kafka connection string from the AppHost resource named `message-broker` and configures `Common.SharedKernel.Messaging` with the Kafka provider.

## Optional Local Host Aliases

To add stable local host names on Windows, run PowerShell as Administrator:

```powershell
cd E:\Scratch\aspnetrun-microservices
.\scripts\update-hosts.ps1
```

To map aliases to a specific Docker host IP:

```powershell
.\scripts\update-hosts.ps1 -IpAddress 192.168.31.47
```

The script is idempotent and creates a timestamped backup of the hosts file before editing.

## OpenTelemetry To OpenSearch

The repository now routes OpenTelemetry signals through an OpenTelemetry Collector bridge:

1. Services emit OTLP using `OTEL_EXPORTER_OTLP_ENDPOINT`.
2. AppHost runs `otel-collector` (contrib image) and exposes OTLP ports.
3. Collector exports logs and traces into OpenSearch indexes.

Collector config location:

```text
src/aspire/aspnetrun-microservices.AppHost/otel-collector-config.yaml
```

Default telemetry indexes:

- `otel-logs`
- `otel-traces`

After running AppHost, validate ingestion with OpenSearch APIs:

```powershell
curl http://localhost:9200/_cat/indices?v
curl http://localhost:9200/otel-logs/_search?pretty
curl http://localhost:9200/otel-traces/_search?pretty
```

## Build And Test

Run the full solution:

```powershell
dotnet test aspnetrun-microservices.slnx
```

Run focused test projects:

```powershell
dotnet test tests\UnitTests\Common.SharedKernel.Tests\Common.SharedKernel.Tests.csproj
dotnet test tests\UnitTests\Products.Api.Tests\Products.Api.Tests.csproj
```

If the Aspire AppHost SDK cannot be resolved, verify that NuGet configuration is accessible and restore packages:

```powershell
dotnet restore aspnetrun-microservices.slnx
```

## Current Gaps

- Gateway.Yarp does not yet configure `AddReverseProxy` or `MapReverseProxy`.
- Cart and Order still contain template weather endpoints.
- Discount gRPC still uses the template greeter service.
- Messaging abstractions exist, but concrete publisher/consumer flows are not implemented.
- README instructions now describe the current repository state; older Docker Compose, Ocelot, Basket, Web UI, and MassTransit flows are not present in this codebase.
