# Messaging Implementation Checklist

Track implementation progress for broker-agnostic messaging, versioning, partitioning policy, and destination provisioning.

## Status legend

- [ ] Not started
- [x] Completed

## Phase 0: Architecture Baseline

- [ ] Approve topic granularity policy (event stream first, not API first).
- [ ] Approve partition policy (aggregate key for ordered streams, no eventId partitioning).
- [ ] Approve versioning policy (minor in stream, major via new destination version).
- [ ] Approve provisioning mode policy per environment.
- [ ] Approve ownership model for destination registrations.

## Phase 1: Registration and Governance Contracts

- [x] Introduce `DestinationRegistration` model with required fields.
- [x] Add required field validation (destination, contract, partition strategy, key selector, retention, DLQ policy).
- [x] Add startup validator for incomplete or invalid registrations.
- [x] Add environment-aware enforcement (warn in dev, fail in non-dev).
- [x] Add clear operator-facing validation errors.

## Phase 2: Broker-Agnostic Core Refactor

- [x] Introduce `MessageContractDescriptor` in envelope metadata.
- [x] Introduce neutral routing semantics (`RoutingKey`, `OrderingKey`).
- [x] Introduce destination provisioning abstractions.
- [x] Add backward-compatible adapters for existing publish APIs.
- [x] Deprecate transport-specific metadata in shared contracts.

## Phase 3: Kafka Provider Hardening

- [x] Implement Kafka mapping for ordering/routing semantics.
- [x] Implement destination provisioning using Kafka admin APIs.
- [x] Add capability descriptors for provider features.
- [x] Enforce partition policy for ordered event streams.
- [x] Validate partition count and retention drift at startup.
- [x] Enforce key presence for key-based partitioning strategies at producer publish time.

## Phase 3.1: Outbox Reliability and Metadata Continuity

- [x] Reclaim expired outbox leases for rows stuck in `processing`.
- [x] Refresh lease when reclaiming `processing` rows.
- [x] Clear lease marker when rows transition to `published`.
- [x] Preserve `processed_on_utc` as publish completion timestamp only.
- [x] Persist aggregate-scoped `OrderingKey`/`RoutingKey` from domain event dispatchers into outbox metadata.
- [x] Restore metadata keys from outbox JSON before provider publish.
- [x] Add tests for outbox lease reclaim and publish-state transitions.
- [x] Add tests for aggregate key propagation in domain-to-integration event dispatch.

## Phase 4: Versioning and Migration Enforcement

- [x] Add consumer-declared supported versions.
- [x] Validate producer/consumer compatibility at startup and runtime.
- [x] Add upcaster pipeline for backward-compatible reads.
- [x] Enforce direct cutover policy (legacy topics are discarded, no dual-publish).

## Phase 5: Operational Readiness

- [ ] Add dashboards for publish/consume failures, retries, and DLQ rates.
- [ ] Add alerts for provisioning drift and compatibility violations.
- [ ] Add runbook for destination registration lifecycle.
- [ ] Add runbook for contract version rollout and rollback.
- [ ] Add load test scenarios for ordering guarantees by key.
- [x] Enforce coverage reporting for messaging/outbox code changes in development workflow guidance.

## Phase 6: Multi-Broker Validation (Optional)

- [ ] Implement second provider package (for example RabbitMQ or Azure Service Bus).
- [ ] Add provider contract parity test suite.
- [ ] Validate unchanged application-level publish/consume code.
- [ ] Document capability differences and approved fallbacks.

## Product Domain Baseline (Mandatory)

- [x] Register `products.events.v1` destination.
- [x] Enforce partition key `productId` for product lifecycle events.
- [x] Prohibit `eventId` as partition key for ordered product streams.
- [x] Set `OrderingKey`/`RoutingKey` to `productId` when enqueuing product integration events.
- [ ] Document retention, DLQ, and consumer group standards for product events.
- [ ] Add integration test proving in-order processing per `productId`.

## Tracking

- Owner: Platform Architecture
- Review cadence: Weekly
- Source ADR: `docs/ADR-0001-broker-agnostic-messaging-architecture.md`
