# Naming and conventions

Types and files
- `PascalCase` for classes, interfaces, enums, and file names matching the main public type.
- Interfaces prefixed with `I` (e.g., `IOrderRepository`).

Methods and parameters
- Public methods: `PascalCase`.
- Parameters and private variables: `camelCase`.

Namespaces
- Use `[Solution].[Service].[Layer]` or `[Company].[Product].[Service].[Layer]` and keep namespaces aligned with folder structure.

DTOs and commands
- Suffix DTOs with `Dto` or use `Request`/`Response` for RPC semantics.
