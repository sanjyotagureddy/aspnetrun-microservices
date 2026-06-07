# ADR-0002 Implementation Checklist

## Purpose

Track delivery of recursive payload masking/redaction, log-type classification, and protected payload persistence defined in ADR-0002.

## Related ADR

- `ADR-0002-recursive-payload-protection-and-storage.md`

---

## API Creation Checklist

- [x] Define `IPayloadProtectionPipeline` contract and method signatures.
- [x] Define `IPayloadMaskingEngine` contract for recursive traversal and rule evaluation.
- [x] Define `IPayloadStore` contract with reference-based persistence semantics.
- [x] Define `PayloadRule`, `PayloadRuleAction`, and `PayloadProtectionResult` models.
- [x] Define `PayloadProtectionOptions` with recursion limit, size limit, and strategy defaults.
- [x] Register all new contracts in DI with backward-compatible defaults.

---

## Phase 0: Design and Contracts

### Phase 0 Most Important

- [x] Define `logType` semantics (`trace`, `api`, `error`) and required fields per type.
- [x] Add payload protection contracts (`IPayloadProtectionPipeline`, `IPayloadMaskingEngine`, `IPayloadStore`).
- [x] Define rule model for global field and path-based masking.
- [x] Define strategy model (`Mask`, `PartialMask`, `Hash`, `Remove`, `Custom`).
- [x] Define fail-safe behavior for masking/parsing errors (no raw payload persistence).

### Phase 0 Good to Have

- [ ] Add schema version fields for payload protection metadata.
- [ ] Add extension points for service-specific custom strategy implementations.

---

## Phase 1: Recursive Masking Engine

### Phase 1 Most Important

- [x] Implement recursive traversal for objects, arrays, dictionaries, and nested graphs.
- [x] Implement global field-name matching (case-insensitive).
- [x] Implement path-based matching with wildcard support (`[*]`).
- [x] Support dynamic and `ExpandoObject` payloads.
- [x] Add recursion depth limits and circular reference protection.
- [x] Apply masking/redaction before any sink persistence.

### Phase 1 Good to Have

- [ ] Add optimized fast-path for scalar payloads.
- [ ] Add configurable key-normalization policy for path evaluation.

---

## Phase 2: Middleware and Pipeline Integration

### Phase 2 Most Important

- [x] Extend `RequestLoggingMiddleware` to capture request and response payload candidates.
- [x] Ensure payload capture is guarded by content-type, size, and route policies.
- [x] Route all captured payloads through centralized protection pipeline.
- [x] Ensure `error` logs include exception details and protected exception payload metadata.
- [x] Ensure `trace` logs do not include exception payload fields by default.

### Phase 2 Good to Have

- [ ] Add selective endpoint payload capture policy.
- [ ] Add per-log-type payload capture toggles.

---

## Phase 3: Persistence Strategy

### Phase 3 Most Important

- [ ] Persist searchable log metadata to OpenSearch.
- [x] Store only protected payload reference (`payloadRef`) in log documents for large payloads.
- [x] Implement optional payload store provider abstraction.
- [x] Add payload metadata fields (`payloadHash`, `payloadSize`, `payloadEncoding`, `payloadEncrypted`).

### Phase 3 Good to Have

- [x] Add payload deduplication by hash.
- [ ] Add provider-specific storage adapters (for example MinIO/Azure Blob).

---

## Phase 4: Security and Policy

### Phase 4 Most Important

- [ ] Ensure default sensitive keys remain enabled and merged with configured keys.
- [ ] Add default global keys for credentials, payment, and token fields.
- [ ] Validate that redaction cannot be disabled outside Development without explicit override.
- [ ] Ensure sensitive payload never appears in sink-failure telemetry callbacks.

### Phase 4 Good to Have

- [ ] Add policy diagnostics endpoint (development only) to inspect active redaction rules.

---

## Phase 5: Testing

### Phase 5 Most Important

- [x] Unit tests for nested object masking.
- [x] Unit tests for arrays and collections masking.
- [x] Unit tests for path-based rule matching.
- [x] Unit tests for remove/hash/partial-mask actions.
- [x] Unit tests for circular reference handling.
- [ ] Integration tests for request/response capture with redaction.
- [ ] Integration tests ensuring no raw sensitive data persists.

### Phase 5 Good to Have

- [ ] Property-based tests for randomized nested payload graphs.
- [ ] Compatibility tests for legacy key-only redaction behavior.

---

## Phase 6: Performance and Operations

### Phase 6 Most Important

- [ ] Benchmark recursive masking overhead at representative payload sizes.
- [ ] Benchmark allocations on high-throughput logging path.
- [ ] Add telemetry counters for payload processed, masked, redacted, and dropped.
- [ ] Add alerting guidance for payload processing failures.

### Phase 6 Good to Have

- [ ] Add adaptive payload sampling for high-volume endpoints.

---

## Acceptance Criteria

- [ ] Recursive masking is applied across all supported payload sources.
- [x] Sensitive fields are protected regardless of nesting depth.
- [x] Path-based and global rules both function as configured.
- [ ] Large payloads are stored by reference, not embedded blindly in OpenSearch.
- [x] `trace`, `api`, and `error` records are consistently classified and queryable.
- [x] Existing logging call sites compile unchanged.
