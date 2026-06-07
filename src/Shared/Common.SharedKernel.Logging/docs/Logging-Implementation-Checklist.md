# Logging Implementation Checklist

## Purpose

Implement ADR-0001 as a complete logging-library roadmap, not only hardening slices.

## Principles

- Keep changes additive and backward compatible.
- Keep implementation classes internal.
- Keep secure defaults enabled.
- Keep middleware ownership inside the logging library only.
- Keep request execution non-blocking and fail-open for logging infrastructure failures.

## Configuration Contract

- Additional masking fields are configured at `Logging:Policy:SensitiveKeys`.
- Library defaults remain enabled and are merged with configured keys (case-insensitive set semantics).
- Redaction may only be disabled in Development.
- No API-specific middleware or API-specific logging components are required for custom sensitive keys.

## Most Important (By Phase)

### Phase 0 (Most Important): Baseline and Alignment

- [ ] Verify current component inventory matches ADR (contracts, models, pipeline, sinks, formatters, enrichers, filters).
- [ ] Document runtime flow and confirm actual execution order in code.
- [ ] Align extension points with ADR (`AddSink`, `AddEnricher`, `AddFilter`, formatter extension strategy).

### Phase 1 (Most Important): Core Governance and Safety

- [x] Add `ILogRedactor` abstraction.
- [x] Add default key-based redactor implementation.
- [x] Add `LoggingPolicyOptions` with secure defaults.
- [x] Bind `Logging:Policy` configuration into `LoggingPolicyOptions`.
- [x] Merge `Logging:Policy:SensitiveKeys` with library defaults.
- [x] Apply redaction in pipeline before sink dispatch.
- [x] Integrate `Activity.Current` enrichment for `TraceId`, `SpanId`, and parent linkage.
- [x] Add sink failure counter and callback hook.
- [x] Add startup validation for queue, batch, sink, and formatter options.

### Phase 2 (Most Important): Runtime Integration and Operations

- [x] Implement request logging middleware in `Common.SharedKernel.Logging`.
- [x] Wire middleware through logging library integration point.
- [x] Confirm no API project adds a custom logging middleware class.
- [x] Populate `LogContext` from HTTP metadata (correlation, trace, tenant, user where available).
- [x] Ensure middleware applies redaction policy to request-derived properties.
- [ ] Implement queue full behavior policy with explicit telemetry on dropped logs.
- [ ] Add operational metrics (`logs_written_total`, `logs_failed_total`, `logs_dropped_total`, `queue_depth`, `queue_utilization_percentage`).

### Phase 3 (Most Important): Governance and Performance

- [ ] Add policy checks to prevent redaction disablement outside Development.
- [ ] Add CI governance checks for prohibited service-owned middleware.
- [ ] Validate no synchronous sink writes occur on request execution path.

## Good to Have (By Phase)

### Phase 0 (Good to Have): Baseline and Alignment

- [ ] Add or update docs links so ADR and checklist remain bi-directionally traceable.

### Phase 1 (Good to Have): Core Governance and Safety

- [ ] Add structured exception logging contract/shape.
- [ ] Integrate shared exception metadata mapping (`ErrorCode`, `Category`, `Severity`) where available.
- [ ] Add `EventId` support on log entries and formatter output.
- [ ] Add `SchemaVersion` support on log entries and formatter output.
- [ ] Implement sink-specific bounded retry behavior.

### Phase 2 (Good to Have): Runtime Integration and Operations

- [x] Add middleware excluded routes configuration (`Logging:Middleware:ExcludedRoutes`).
- [ ] Ensure metrics are exposed through shared observability/OpenTelemetry exporters.
- [ ] Confirm retention remains sink-owned and not enforced in core logging library.

### Phase 3 (Good to Have): Governance and Performance

- [ ] Implement optional sampling controls for high-volume logs.
- [ ] Ensure `Error` and `Critical` logs are sampling-exempt.
- [ ] Add performance benchmarks for enqueue latency and bounded memory behavior.

## Test Plan

### Most Important

- [ ] Unit test redaction behavior for default sensitive keys.
- [ ] Unit test configured `SensitiveKeys` merged with defaults.
- [ ] Unit test sink-failure telemetry increments.
- [ ] Unit test queue full behavior and dropped-log metrics.
- [ ] Integration test request middleware context, duration, status, and route logging.
- [ ] Integration test middleware behavior is provided by library, not API code.

### Good to Have

- [ ] Unit test exception message redaction toggle.
- [ ] Unit test stack trace non-redaction default behavior.
- [ ] Unit test event id and schema version emission.
- [ ] Unit test retry behavior bounds per sink.
- [ ] Integration test middleware exclusions (`/health`, `/metrics`, `/swagger`).
- [ ] Integration test OpenTelemetry activity enrichment.
- [ ] Benchmark test for P95 enqueue latency target.

## Acceptance Criteria

- [ ] Sensitive values are masked by default.
- [ ] User-defined keys from `Logging:Policy:SensitiveKeys` are masked without API code changes.
- [ ] Structured exceptions include standard fields and shared exception metadata.
- [ ] `EventId` and `SchemaVersion` are available in emitted log events.
- [ ] OpenTelemetry context is consistently present on log events.
- [ ] Queue backpressure behavior is bounded and observable.
- [ ] Sink failures are observable via telemetry hook and metrics.
- [ ] Logging middleware lives only in `Common.SharedKernel.Logging`.
- [ ] No separate custom logging middleware exists in API projects.
- [ ] Existing logging call sites compile unchanged.
- [ ] Performance objectives are validated by benchmark results.
