# ADR-0001: Enterprise Logging Architecture & Hardening

## Status
Proposed

## Tracking

- Implementation Checklist: `Logging-Implementation-Checklist.md`

## Date

2026-06-07

---

# 1. Purpose

This ADR defines the architecture, responsibilities, governance model, operational behavior, and implementation standards for the shared logging library.

The goal is to provide a lightweight, extensible, production-ready logging platform that can be consistently adopted across all services while maintaining strong observability, security, and operational reliability.

The library must:

- Support structured logging.
- Support multiple sinks.
- Provide centralized governance.
- Integrate with tracing and observability platforms.
- Protect sensitive information.
- Remain lightweight and easy to adopt.
- Avoid service-specific logging implementations.

---

# 2. System Overview

Current library responsibilities:

- Logger contracts and dependency injection registration.
- Log pipeline orchestration.
- Enrichment.
- Filtering.
- Formatting.
- Sink dispatching.
- Context propagation.
- Background asynchronous processing.

## Contracts

- `ILogger`
- `ILogger<T>`
- `ILoggingFactory`
- `ILoggingBuilder`
- `ILogFilter`
- `ILogEnricher`
- `ILogFormatter`
- `ILogContextAccessor`

## Core Models

- `LogEntry`
- `LogContext`
- `LogEnrichmentContext`
- `LoggingOptions`
- Sink-specific options

## Pipeline Components

- `LoggingPipeline`
- `LogDispatcher`
- `LoggingHostedService`

## Composition Root

- `AddCommonSharedKernelLogging(...)`
- `LoggingBuilder`
- `LoggingFactory`
- `Logger`

## Built-In Sinks

- `ConsoleLogSink`
- `FileLogSink`
- `ElasticsearchLogSink`

## Built-In Formatters

- `JsonLogFormatter`
- `TextLogFormatter`

## Built-In Enrichers

- `CorrelationEnricher`
- `TraceEnricher`
- `EnvironmentEnricher`
- `MachineEnricher`
- `TenantEnricher`
- `UserEnricher`

## Built-In Filters

- `MinimumLevelFilter`
- `CategoryPrefixFilter`
- `PropertyFilter`

---

# 3. Runtime Flow

1. Application resolves `ILogger` or `ILogger<T>`.
2. Logger forwards events to `LoggingPipeline`.
3. Pipeline merges contextual properties.
4. Enrichers execute.
5. Filters execute.
6. Redaction executes.
7. Event enters dispatcher queue.
8. Background service flushes batches.
9. Sinks write events.
10. Metrics and telemetry are updated.

---

# 4. Architectural Principles

## Simplicity

The logging library should remain lightweight and easy to understand.

## Security by Default

Sensitive data protection must be enabled by default.

## Fail Open

Application execution must not fail because logging infrastructure fails.

## Observability First

Logging failures must be visible through telemetry.

## Extensibility

Consumers should be able to add sinks, enrichers, and filters without modifying core components.

## Backward Compatibility

Existing logging call sites should not require modification.

---

# 5. Redaction

## Contract

```csharp
public interface ILogRedactor
{
    LogEntry Redact(LogEntry entry);
}
```

## Default Sensitive Keys

- password
- token
- secret
- apiKey
- authorization
- cookie

### Behavior

- Matching is case-insensitive.
- Structure is preserved.
- Values are replaced with `"***"`.

Example:

```json
{
  "password": "***"
}
```

---

# 6. Logging Policy

```csharp
public sealed record LoggingPolicyOptions
{
    public bool EnableRedaction { get; set; } = true;

    public bool RedactExceptionMessages { get; set; } = true;

    public IReadOnlySet<string> SensitiveKeys { get; init; }
}
```

### Configuration

```json
{
  "Logging": {
    "Policy": {
      "SensitiveKeys": [
        "customerEmail",
        "phoneNumber",
        "cardLast4"
      ]
    }
  }
}
```

### Behavior

- Library defaults are always included.
- Service-specific keys are merged.
- Case-insensitive matching is applied.

---

# 7. Event Identity

Every log event should support a stable event identifier.

Examples:

- `PRODUCT_CREATED`
- `PRODUCT_CREATION_FAILED`
- `ORDER_PAYMENT_TIMEOUT`
- `USER_AUTHENTICATION_FAILED`

```csharp
public sealed class LogEntry
{
    public string? EventId { get; set; }
}
```

### Purpose

- Simplified querying.
- Easier dashboard creation.
- More reliable alerting.
- Reduced dependency on message text.

---

# 8. Structured Exception Logging

Exceptions should be emitted as structured data.

### Standard Fields

- ExceptionType
- ExceptionMessage
- StackTrace
- InnerException
- ErrorCode
- Category
- Severity

Integration with the shared exception library should automatically populate:

- ErrorCode
- Category
- Severity

### Rules

- Stack traces must not be redacted by default.
- Exception messages may be redacted based on policy configuration.

---

# 9. OpenTelemetry Integration

The logging library must integrate with OpenTelemetry `Activity` context.

### Captured Fields

- TraceId
- SpanId
- ParentSpanId
- CorrelationId

Source:

```csharp
Activity.Current
```

### Requirements

- OpenTelemetry remains the source of truth.
- The logging library must not implement a parallel tracing model.
- Logging context must automatically enrich entries from the current Activity.

---

# 10. Filtering

Existing filter contracts remain unchanged.

### Supported Filters

- Minimum level filtering
- Category filtering
- Property filtering
- Policy-driven filtering

### Processing Order

Filtering occurs before dispatch and before sink writes.

---

# 11. Dispatcher Queue

The dispatcher queue must remain bounded.

Queue capacity must be configurable.

## Queue Full Behavior

```csharp
public enum QueueFullBehavior
{
    DropNewest,
    DropOldest,
    Block,
    Fail
}
```

### Default

```text
DropNewest
```

### Requirements

- Prevent unbounded memory growth.
- Increment dropped-log metrics.
- Emit warning telemetry periodically.

---

# 12. Log Sampling

Optional sampling may be applied to high-volume events.

### Goals

- Prevent log storms.
- Protect sinks.
- Reduce storage costs.

### Requirements

- Error logs must never be sampled.
- Critical logs must never be sampled.
- Sampling statistics must be observable.

### Example

```csharp
public sealed record SamplingOptions
{
    public bool Enabled { get; set; }

    public int MaxEventsPerSecond { get; set; }

    public LogLevel MinimumLevelExempt { get; set; }
        = LogLevel.Error;
}
```

---

# 13. Sink Failure Telemetry

Sink failures must never be silently ignored.

### Capabilities

- Failure counters
- Warning callbacks
- Failure metrics
- Periodic warning events

### Callback

```csharp
Action<Exception, string sinkName>
```

### Rules

- Sink failures must not crash applications.
- Sink failures must be observable.

---

# 14. Metrics

The logging library should expose operational metrics.

### Required Metrics

- `logs_written_total`
- `logs_failed_total`
- `logs_dropped_total`
- `queue_depth`
- `queue_utilization_percentage`

### Integration

Metrics should integrate with the observability library and OpenTelemetry metrics exporters.

---

# 15. Retry Behavior

Retry behavior is sink-specific.

### Console Sink

- No retry

### File Sink

- Limited retry

### Elasticsearch Sink

- Exponential backoff retry

### Requirements

- Retries must be bounded.
- Retries must never block request execution.
- Retry failures must update telemetry.

---

# 16. Startup Guardrails

Validate configuration during startup.

### Validation Rules

- Queue capacity must be greater than zero.
- Batch size must be greater than zero.
- Sink configuration must be valid.
- Formatter configuration must be valid.

### Warnings

- Redaction disabled outside Development.

### Failure Policy

Invalid configuration should fail fast during startup.

---

# 17. Middleware Ownership

HTTP request logging belongs to the logging library.

Service teams must not implement custom request logging middleware.

Registration:

```csharp
services.AddCommonSharedKernelLogging(...)
```

The library owns:

- Request start logging
- Request completion logging
- Duration tracking
- Context propagation
- Redaction application

---

# 18. Middleware Capabilities

The middleware should capture:

- HTTP method
- Route template
- Response status code
- Duration
- Correlation identifiers
- Tenant information
- User information

## Route Exclusions

```json
{
  "Logging": {
    "Middleware": {
      "ExcludedRoutes": [
        "/health",
        "/metrics",
        "/swagger"
      ]
    }
  }
}
```

### Excluded Endpoints

- Health checks
- Metrics endpoints
- Swagger endpoints

---

# 19. Context Propagation

The logging library must support scoped contextual properties.

Example:

```csharp
using (logger.BeginScope(new
{
    OrderId = orderId,
    CustomerId = customerId
}))
{
}
```

### Requirements

- Scope properties must flow automatically.
- Scope properties must be available to enrichers and sinks.

---

# 20. Audit Logging Boundary

This library is not an audit logging solution.

### Audit Examples

- User permission changes
- Financial transactions
- Account ownership transfers
- Security policy changes

These events must be handled by the dedicated audit logging library.

---

# 21. Retention Ownership

Retention belongs to sinks.

### Examples

- Elasticsearch ILM
- File rolling policies
- Cloud retention policies

The logging library may expose metadata but must not enforce retention.

---

# 22. Schema Versioning

All log entries should include a schema version.

Example:

```csharp
SchemaVersion = "1.0";
```

### Purpose

- Safe schema evolution
- Backward compatibility
- Stable ingestion pipelines

### Versioning Strategy

Follow semantic versioning principles.

---

# 23. Performance Objectives

The logging library must remain suitable for high-throughput services.

### Targets

- Non-blocking request path
- P95 enqueue latency < 1 ms
- Bounded memory usage
- No synchronous sink writes during request execution

### Validation

Performance regressions should be detected through automated benchmarks.

---

# 24. Extensibility

Supported extension points:

### Sinks

```csharp
builder.AddSink(...)
```

### Enrichers

```csharp
builder.AddEnricher(...)
```

### Filters

```csharp
builder.AddFilter(...)
```

### Formatters

```csharp
builder.AddFormatter(...)
```

### Requirements

- Backward compatibility must be preserved.
- Core abstractions must remain provider-agnostic.

---

# 25. Migration Strategy

## Phase 1

Implement:

- Redaction
- Logging policy
- Structured exception logging
- OpenTelemetry integration
- Sink telemetry

## Phase 2

Implement:

- Request middleware
- Route exclusions
- Queue backpressure controls
- Metrics

## Phase 3

Implement:

- Sampling
- Governance validation
- CI policy enforcement
- Performance benchmarks

---

# 26. Consequences

## Positive

- Consistent logging across services.
- Improved security posture.
- Better observability.
- Lower operational risk.
- Easier troubleshooting.
- Standardized governance.

## Trade-Offs

- Slight increase in per-log processing.
- Additional configuration surface area.
- More operational metrics to maintain.

---

# 27. Alternatives Considered

## Full Policy DSL

Rejected.

Reason:

- Excessive complexity.

## Service-Owned Logging Middleware

Rejected.

Reason:

- Inconsistent implementations.

## Fail-Fast Sink Failures

Rejected.

Reason:

- Logging must not destabilize applications.

## Service-Owned Redaction

Rejected.

Reason:

- High risk of inconsistent security controls.

---

# 28. Implementation Guardrails

- Keep implementation classes internal.
- Keep redaction deterministic.
- Keep dependencies minimal.
- Keep defaults secure.
- Maintain backward compatibility.
- Prefer composition over inheritance.
- Avoid provider-specific behavior in core abstractions.

---

# 29. Resolved Decisions

## Exception Stack Traces

Stack traces will not be redacted by default.

Only exception messages and structured properties are eligible for redaction.

## Strict Mode

Current behavior:

- Mask sensitive values.
- Do not drop log events.

Future strict-mode support may be evaluated.

## Redaction Disablement

Redaction may only be disabled in Development environments.

The following environments must keep redaction enabled:

- QA
- UAT
- Staging
- Pre-Production
- Production

---

# 30. Target State

The final logging library provides:

## Core Capabilities

- Structured logging
- Async dispatching
- Multi-sink support
- Formatting
- Enrichment
- Filtering

## Governance

- Redaction
- Policy enforcement
- Startup validation

## Observability

- OpenTelemetry integration
- Metrics
- Failure telemetry

## Platform Integration

- HTTP request logging
- Context propagation
- Shared exception integration

## Operational Safety

- Queue backpressure handling
- Retry policies
- Sampling controls

The result is a secure, observable, extensible, enterprise-grade logging platform suitable for all services across the platform.