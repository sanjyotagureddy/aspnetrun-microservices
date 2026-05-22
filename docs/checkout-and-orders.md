# Checkout and Orders — Architecture Patterns

This document captures recommended patterns and invariants for the checkout, ordering, and inventory flows.

## Goals
- Ensure correctness of inventory and payment interactions.
- Support retries and partial failures with clear compensating actions.
- Keep user experience responsive while maintaining eventual consistency.

## Idempotency
- All commands that create or mutate order state (checkout, payment capture) must accept and honor an `Idempotency-Key` header.
- Persist idempotency entries (request hash → response) for a TTL matching gateway retries.

## Reservation / Inventory Hold
- Use a two‑phase flow: reserve (hold) inventory during checkout, and finalize on successful payment capture.
- Implement short TTL holds with background release jobs to free expired reservations.

## Transactional Outbox
- Use a transactional outbox in the same DB transaction that writes domain changes (order created, reservation taken) to guarantee at‑least‑once delivery to the message broker.
- A background publisher reads the outbox and publishes messages to a broker (Rabbit/Kafka). Mark outbox rows as published on success.

## Saga / Orchestration
- Model multi‑step flows (checkout → payment → capture → shipping) as Sagas. Prefer orchestration (dedicated saga coordinator) or choreography depending on complexity.
- Provide compensating actions (release inventory, refund) for failures.

## Order State Model (recommended)
- `Pending` (checkout started / reserved) → `AwaitingPayment` → `Paid` → `Fulfilled` → `Completed`
- `Cancelled`, `Failed`, `Refunded` as terminal states with audit trail.

## Events & Ids
- Use stable event names (OrderCreated, PaymentCaptured, InventoryReleased) and include correlation IDs and idempotency keys.
- Event consumers must be idempotent.

## External Integrations
- Payments: use asynchronous webhook confirmation and reconcile with idempotency and transaction logs.
- Shipping: publish `OrderReadyForFulfillment` event and reconcile carrier responses.

## Recommended tech choices
- Broker: RabbitMQ or Kafka (depends on throughput and ordering needs).
- Persistence: ensure strong consistency for reservation data (SQL or strongly consistent store) and outbox table co-located with domain writes.

## Operational notes
- Add health checks for background outbox publisher and saga coordinator.
- Add monitoring for outbox backlog, reservation expirations, and payment failure rates.

--
Link this doc from `docs/architecture.md` and service README files that own order/payment flows.
