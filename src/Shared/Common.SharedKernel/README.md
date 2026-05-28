# Common.SharedKernel

Shared kernel types are grouped by purpose so the boundary stays small and easy to navigate.

## Folder layout

- `Common/` for shared helpers such as guards.
- `Results/` for result-pattern primitives.
- `Abstractions/Entities/` for entity and aggregate-root base types.
- `Abstractions/Auditing/` for audit metadata support.
- `Abstractions/ValueObjects/` for value-object equality support.
- `Abstractions/Events/` for domain-event contracts and base types.
- `Abstractions/IntegrationEvents/` for integration-event contracts and base types.
- `Exceptions/` for shared exception types.
- `Messaging/` for event and messaging contracts, including the transport-neutral publisher abstraction.
- `Logging/` for logging abstractions and log entry contracts.
- `Caching/` for caching abstractions.
- `Http/` for shared HTTP abstractions.
- `Security/` for security abstractions.
- `Validation/` for validation abstractions.
- `Observability/Correlation/` for app-call context base types, ambient scope, and tracing support.

The initial implementation currently includes shared exceptions, message base contracts, a transport-neutral publisher abstraction, logging contracts, and correlation primitives; the remaining folders are reserved for future primitives.

Future folders can follow the same split for exceptions, messaging, logging, caching, HTTP, security, validation, and observability primitives.
