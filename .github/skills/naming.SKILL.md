# Naming (skill)

Purpose: concise, machine-friendly naming rules and examples.

Type/file naming
- Classes, structs, enums, exceptions: `PascalCase` (e.g., `OrderAggregate`, `OrderPlacedEvent`).
- Interfaces: `I` prefix + `PascalCase` (e.g., `IOrderRepository`).
- Files: match the main public type name (e.g., `CreateOrderCommand.cs`).

Namespaces
- Use `[Solution].[Service].[Layer]` or `[Company].[Product].[Service].[Layer]` and keep namespaces aligned with folder structure.

DTOs / Commands / Queries
- DTOs: suffix with `Dto` (e.g., `OrderDto`).
- Commands/Queries: suffix with `Command`/`Query` (e.g., `CreateOrderCommand`).

Api routes and versioning
- Use noun-based resource routes: `/api/v1/orders`.
- Include API version in route and OpenAPI metadata.

Configuration keys
- Use colon-separated hierarchical keys (e.g., `ConnectionStrings:OrderingDb`).

Database objects
- Table names: plural, `snake_case` for SQL (e.g., `orders`), or follow project DB convention consistently.

