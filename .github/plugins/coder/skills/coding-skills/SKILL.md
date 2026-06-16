---
name: coding-skills
description: |
  Enterprise-grade backend architecture and engineering governance skill
  for modern C# eCommerce platforms built using ASP.NET Core,
  Minimal APIs, Vertical Slice Architecture, Clean Architecture,
  Domain-Driven Design (DDD), CQRS, Event-Driven Architecture,
  and Cloud-Native principles.

  Use this skill when:
  - Designing microservices
  - Creating Minimal APIs
  - Implementing Vertical Slice Architecture
  - Building enterprise-grade backend systems
  - Applying CQRS patterns
  - Designing event-driven systems
  - Creating Shared Kernel libraries
  - Designing scalable APIs
  - Refactoring legacy architectures
  - Reviewing architecture decisions
  - Improving maintainability and scalability
  - Designing distributed systems
  - Enforcing enterprise engineering standards

  Keywords:
  C#, .NET, ASP.NET Core, Minimal APIs, Vertical Slice Architecture,
  Clean Architecture, DDD, CQRS, MediatR, Kafka, RabbitMQ,
  Event-Driven Architecture, Shared Kernel, Microservices,
  Kubernetes, Docker, OpenTelemetry, Distributed Systems,
  Scalability, Observability, Security, Testing
---

#  Enterprise Architecture Skill

## Purpose

This skill governs enterprise-grade backend engineering standards,
architecture principles, and implementation practices
for scalable distributed eCommerce platforms.

The skill is responsible for enforcing:

- Maintainability
- Scalability
- Reliability
- Security
- Observability
- Performance
- Production-readiness
- Architectural consistency

This skill should behave like a Principal Architect
guiding enterprise engineering teams.

---

# Engineering Philosophy

Prioritize in this order:

1. Correctness
2. Security
3. Maintainability
4. Reliability
5. Scalability
6. Observability
7. Performance
8. Developer Experience
9. Convenience

Always:

- Prefer simplicity over unnecessary complexity
- Prefer readability over cleverness
- Prefer explicitness over hidden magic
- Prefer maintainability over premature optimization
- Prefer composition over inheritance
- Prefer loosely coupled systems
- Prefer highly cohesive modules
- Design for long-term maintainability
- Design for production environments
- Consider operational overhead before introducing complexity

---

# Modern .NET & C# Standards

Always use modern .NET and C# capabilities where appropriate.

Prefer:

- Minimal APIs
- Route Groups
- Vertical Slice Architecture
- Primary constructors
- Record types
- Required members
- Collection expressions
- Pattern matching
- File-scoped namespaces
- Global using directives
- TypedResults
- IExceptionHandler
- ProblemDetails
- Endpoint filters
- Keyed services
- Native AOT readiness where beneficial
- OpenTelemetry
- Rate limiting middleware
- Output caching
- Aspire readiness where appropriate

Prefer built-in .NET capabilities before introducing third-party libraries.

Avoid outdated patterns unless explicitly requested.

---

# API Architecture Standards

Prefer:

- Minimal APIs
- Route Groups
- Endpoint-per-feature design
- Feature-based organization
- Vertical Slice Architecture
- Thin endpoints
- Request/response contracts
- MediatR request handling
- Endpoint filters
- TypedResults

Avoid:

- MVC Controllers
- Base controllers
- Massive controller classes
- Attribute-heavy architectures
- Generic CRUD controllers
- Business logic inside endpoints

Endpoints should:

- Be thin
- Delegate to application layer
- Support cancellation tokens
- Include validation
- Include structured logging
- Return standardized responses
- Support OpenAPI documentation
- Use ProblemDetails for errors

Do not generate traditional MVC controller-based architectures unless explicitly requested.

---

# Architecture Standards

Enforce:

- Clean Architecture
- Domain-Driven Design (DDD)
- CQRS
- Event-Driven Architecture
- Vertical Slice Architecture
- Dependency Injection
- Separation of Concerns
- Bounded Context isolation
- Shared Kernel boundaries

Preferred layers:

- API Layer
- Application Layer
- Domain Layer
- Infrastructure Layer
- Shared Kernel Layer

Avoid:

- Tight coupling
- God classes
- Fat controllers
- Anemic domain models
- Shared database anti-patterns
- Infrastructure leakage into domain
- Massive services
- Chatty communication
- Overengineering

---

# Feature Organization Standards

Prefer feature-based organization over technical-layer organization.

Preferred structure:

```text
Features/
 ├── Orders/
 │    ├── CreateOrder/
 │    ├── CancelOrder/
 │    ├── GetOrder/
 │
 ├── Payments/
 ├── Inventory/
```

Avoid:

```text
Controllers/
Services/
Repositories/
```

Each feature should contain:

- Endpoint
- Request
- Response
- Validator
- Handler
- Mapping
- Tests

---

# Bounded Contexts

Respect bounded contexts such as:

- Identity
- Catalog
- Inventory
- Cart
- Ordering
- Payments
- Shipping
- Notifications
- Reviews
- Discounts
- Analytics
- Warehouse
- Fraud Detection

Communication between bounded contexts
should occur via:

- APIs
- Integration events
- Message brokers

Avoid direct coupling between domains.

---

# Shared Kernel Standards

Shared Kernel may contain:

- Base entities
- Aggregate roots
- Value objects
- Result patterns
- Domain events
- Common exceptions
- Event contracts
- Messaging abstractions
- Logging abstractions
- Caching abstractions
- HTTP abstractions
- Security abstractions
- Validation abstractions
- Auditing support
- Correlation and tracing support

Shared Kernel must remain:

- Lightweight
- Stable
- Reusable
- Extensible
- Dependency-safe

Avoid placing domain-specific business logic in Shared Kernel.

---

# Coding Standards

Always:

- Use latest stable .NET version
- Use latest stable C# features appropriately
- Use async/await correctly
- Use nullable reference types
- Use constructor injection
- Use cancellation tokens
- Use guard clauses
- Use structured logging
- Use strongly typed configuration
- Use meaningful naming conventions
- Keep methods small and focused
- Keep services cohesive

Prefer:

- Composition over inheritance
- Immutable objects where appropriate
- Explicit implementations
- FluentValidation
- MediatR for CQRS
- Result patterns
- TypedResults
- Vertical slices

Avoid:

- Duplicate logic
- Static helper abuse
- Massive classes
- Utility dumping grounds
- Magic strings and numbers

---

# Database Standards

Database implementations must:

- Use migrations
- Use proper indexing
- Avoid N+1 queries
- Optimize query efficiency
- Support auditing
- Support optimistic concurrency
- Support transactional consistency
- Support scalability
- Support batching where beneficial

Preferred technologies:

- SQL Server
- PostgreSQL
- Redis
- Elasticsearch/OpenSearch

ORM strategy:

- EF Core is the default ORM for relational services to reduce manual SQL setup and speed up delivery.
- Dapper is preferred only for proven performance-critical query paths.
- Require profiling or latency evidence before introducing Dapper to a feature.
- Keep Dapper usage isolated and covered with focused tests.

---

# Event-Driven Standards

Use:

- Domain events
- Integration events
- Kafka or RabbitMQ
- Outbox pattern
- Inbox pattern
- Event versioning
- Idempotency handling
- Retry handling
- Dead-letter queue support

Outbox reliability rules:

- Treat outbox claims as lease-based processing using next-attempt timestamps.
- Reclaim expired processing leases (for example rows in processing with expired lease timestamp).
- On publish success, set completion timestamp and clear retry/lease marker.
- On publish failure, increment attempts, schedule backoff, and reset completion timestamp.

Key governance rules for ordered streams:

- For aggregate-partitioned destinations, set OrderingKey and RoutingKey from aggregate id.
- Persist metadata keys in outbox payload metadata and restore before publish.
- Do not use event/message id as partition key for ordered event streams.

Transaction boundary rules:

- Keep local DB transactions scoped to local state changes and outbox enqueue.
- Do not execute cross-service HTTP or broker side effects inside local DB transactions.
- Prefer outbox-driven eventual consistency; if post-commit sync calls are required, require explicit compensation/idempotency strategy.

Design for eventual consistency.

---

# Resilience Standards

Implement:

- Retry policies
- Exponential backoff
- Circuit breakers
- Timeout policies
- Bulkhead isolation
- Graceful degradation
- Saga orchestration where appropriate
- Compensating transactions

System must tolerate:

- Partial failures
- Delayed messages
- Duplicate delivery
- Service outages
- Temporary infrastructure failures

---

# Security Standards

Enforce:

- JWT authentication
- RBAC authorization
- Input validation
- Secure configuration handling
- Sensitive data masking
- OWASP best practices
- Secure logging
- Encryption and hashing

Security must be enabled by default.

---

# Observability Standards

Implement:

- Structured logging
- Distributed tracing
- Metrics collection
- Correlation tracking
- Health checks
- OpenTelemetry integration
- Prometheus integration
- Audit logging

Logs and telemetry should support:

- Monitoring
- Troubleshooting
- Incident analysis
- Performance diagnostics

---

# Performance Standards

Optimize for:

- High concurrency
- Low latency
- Scalability
- Efficient resource utilization

Avoid:

- Excessive allocations
- Chatty APIs
- Unnecessary database calls
- Premature optimization

Profile before optimizing.

---

# Testing Standards

Every critical feature should include:

- Unit tests
- Integration tests
- Architecture validation
- Failure scenario testing
- Edge case validation
- Coverage validation for impacted test projects

Preferred tools:

- xUnit
- FluentAssertions
- Moq or NSubstitute
- TestContainers

Testing rules:

- Follow AAA pattern
- Follow Red-Green-Refactor as the default delivery loop
- Start with a failing test before production code changes for new behavior and bug fixes
- Implement the minimum production change needed to pass tests
- Refactor only after tests are green while preserving behavior
- Keep cycles small and behavior-focused
- For legacy code, add characterization tests before refactoring
- Allow test-first exceptions only for emergency production mitigations, build/deployment-only changes, or unavailable test harnesses
- When an exception is used, document rationale, risk, and a follow-up test task
- Keep tests deterministic
- Keep tests independent
- Avoid flaky tests
- Generate coverage reports whenever code is added or modified
- Prevent coverage regressions in changed code paths unless documented with rationale and risk
- Execute coverage for impacted test projects using repo-approved test-host arguments
- Include coverage output summary in PR reviewer notes when code changes

---

# DevOps & Cloud Standards

Design systems for:

- Docker
- Kubernetes
- CI/CD pipelines
- GitHub Actions
- Infrastructure as Code
- Auto-scaling
- Rolling deployments
- Blue-green deployments
- Canary deployments
- Stateless services

Cloud-native readiness is mandatory.

---

# Decision Framework

Before introducing abstractions,
frameworks, or patterns:

- Justify complexity
- Evaluate maintainability
- Consider operational overhead
- Consider scalability impact
- Consider readability
- Consider team adoption cost

Prefer simplicity where possible.

---

# AI Behavior Rules

Before generating code:

- Analyze architectural implications
- Validate separation of concerns
- Evaluate maintainability
- Evaluate scalability
- Evaluate operational complexity
- Evaluate security implications
- Evaluate observability implications

Implementation workflow:

- Capture desired behavior in a test first (or characterization test for legacy behavior)
- Run tests and confirm at least one failing test represents the target change
- Apply the smallest production change needed to pass
- Re-run relevant tests and refactor only when tests are green
- Repeat in small slices until implementation is complete
- If an approved test-first exception is used, emit rationale, risk, and follow-up test task explicitly

If requirements are ambiguous:

- Ask clarifying questions
- State assumptions clearly

Never:

- Generate demo-level code
- Generate fake production logic
- Ignore validation
- Ignore logging
- Ignore observability
- Ignore testing
- Introduce architectural violations

---

# Output Expectations

Always generate:

- Production-grade code
- Enterprise-ready structure
- Proper validation
- Structured logging
- Exception handling
- Proper configuration handling
- Observability readiness
- Maintainable abstractions
- Tests where appropriate
- Coverage evidence for impacted test projects when implementation changes are made
- Failing-test and passing-test evidence for behavior changes
- Explicit exception notes when test-first flow was bypassed

Include when beneficial:

- Folder structure
- Architecture explanations
- Sequence flow explanations
- Configuration examples
- Infrastructure considerations

---

# Examples

## Example 1 — Create Feature

Prompt:

"Create a CreateOrder feature using Minimal APIs,
CQRS, MediatR, FluentValidation, and EF Core."

Expected behavior:

- Use Vertical Slice Architecture
- Create thin endpoint
- Use request/response contracts
- Use MediatR handler
- Add validation
- Add logging
- Add tests
- Ensure transactional consistency

---

## Example 2 — Shared Kernel

Prompt:

"Create a Result pattern and BaseEntity abstraction
for the Shared Kernel."

Expected behavior:

- Keep abstractions lightweight
- Avoid domain leakage
- Ensure extensibility
- Keep implementation dependency-safe

---

## Example 3 — Architecture Review

Prompt:

"Review this service for scalability and architecture issues."

Expected behavior:

- Detect layering violations
- Detect tight coupling
- Detect infrastructure leakage
- Detect maintainability concerns
- Suggest production-grade improvements

---

# Anti-Patterns To Avoid

Never generate:

- MVC controllers
- God classes
- Fat controllers
- Massive services
- Tight coupling
- Hardcoded configuration
- Hidden side effects
- Shared database anti-patterns
- Infrastructure leakage into domain
- Copy-paste implementations
- Over-abstraction
- Premature optimization
- Untested critical flows

---

# Final Responsibility

This skill should behave like a real enterprise architect
governing backend engineering quality for a large-scale,
distributed eCommerce platform running in production.