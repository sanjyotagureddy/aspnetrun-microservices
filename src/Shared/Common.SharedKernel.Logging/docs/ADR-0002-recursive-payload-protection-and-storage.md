# ADR-0002: Recursive Payload Protection, Classification, and Storage Strategy

## Status

Proposed

## Tracking

- Implementation Checklist: `ADR-0002-Implementation-Checklist.md`

## Date

2026-06-07

---

## 1. Context

The logging framework must guarantee sensitive data protection before persistence.

Current redaction behavior is key-based at the first property level and does not provide full recursive traversal and rule-based payload processing for nested content.

The platform also needs a clear operational classification model and persistence strategy for structured logs and optional large payloads.

---

## 2. Decision

## 2.1 Log Classification

Adopt three operational log types as first-class fields:

- `trace`: regular diagnostic flow, no exception payload required.
- `api`: HTTP request and response lifecycle records.
- `error`: failure records with exception details.

`audit` is deferred and will be addressed by a separate ADR.

## 2.2 Payload Protection Model

Introduce a centralized payload protection pipeline that executes before sink persistence.

Required stages:

1. Serialize object payload.
2. Parse payload structure.
3. Apply recursive masking rules.
4. Apply redaction/removal rules.
5. Validate payload size limits.
6. Optionally compress.
7. Optionally encrypt.
8. Persist protected payload.
9. Generate payload reference.
10. Store reference in log record.

Raw sensitive payloads must never be persisted when protection is enabled.

## 2.3 Masking Scope

Protection rules apply to all payload sources, including:

- HTTP requests
- HTTP responses
- Trace payloads
- Exception payloads
- Business events
- Kafka messages
- State snapshots
- Custom payload fields

## 2.4 Supported Structures

The masking engine must support recursive traversal across:

- JSON objects and nested objects
- Arrays and collections
- Dictionaries
- Dynamic and `ExpandoObject`
- POCO models and complex object graphs

## 2.5 Rule Types

Support both:

- Global field-name rules (for example: `password`, `email`, `cardNumber`, `authorization`)
- Path-based rules (for example: `customer.email`, `orders[*].cardNumber`)

## 2.6 Redaction Strategies

Support strategy per rule:

- Mask
- PartialMask
- Hash
- Remove
- Custom

## 2.7 Persistence Strategy

- Primary searchable store: OpenSearch indexes (`api-logs-*`, `infra-logs-*`, `messaging-log-*`).
- Optional payload store: object storage provider for large protected payloads.
- Log documents should store payload references and essential metadata; large payload bodies should not be duplicated in OpenSearch.

---

## 3. Architectural Shape

## 3.1 New Contracts

- `IPayloadProtectionPipeline`
- `IPayloadMaskingEngine`
- `IPayloadRuleEvaluator`
- `IPayloadStore`

## 3.2 New Models

- `PayloadProtectionOptions`
- `PayloadRule`
- `PayloadRuleAction`
- `PayloadProtectionResult`

## 3.3 Integration Points

- `RequestLoggingMiddleware` for request/response payload capture and protection.
- `DefaultLogRedactor` as centralized recursive redaction entry point.
- `LoggingPipeline` to enforce redaction before dispatch.

---

## 4. Performance and Safety Requirements

The implementation must:

- Avoid excessive allocations.
- Support configurable recursion depth.
- Handle circular references safely.
- Enforce payload size limits.
- Fail safe (drop or sanitize payload on processing error).
- Keep request path non-blocking and fail-open for core application flow.

---

## 5. Backward Compatibility

- Existing logger call sites remain unchanged.
- Existing `Logging:Policy:SensitiveKeys` remains supported.
- Recursive masking is additive and applied centrally.

---

## 6. Non-Goals

- Audit/event-retention governance is out of scope for this ADR.
- Sink-specific retention lifecycle policies remain sink-owned.

---

## 7. Consequences

## Positive

- Stronger sensitive data guarantees.
- Consistent masking across all payload sources.
- Better queryability using stable `logType` semantics.

## Trade-offs

- Higher implementation complexity in redaction path.
- Additional test and benchmark burden.
- Potential payload processing overhead requiring tuning.

---

## 8. Acceptance Summary

This ADR is accepted when:

- Recursive masking and path/global rules are implemented and tested.
- Request/response payload capture flows through centralized protection.
- Protected payloads are persisted without raw sensitive data leakage.
- Log records consistently include `logType` and required operational fields.
