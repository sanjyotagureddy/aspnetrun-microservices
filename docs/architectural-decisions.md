# Architectural Decisions (ADRs)

This file contains Architectural Decision Records (ADR) documenting important design decisions and approved exceptions.

Template

```
Title: <short-title>
Status: Proposed | Accepted | Deprecated | Rejected
Context:
  - <background and constraints>
Decision:
  - <what was decided>
Consequences:
  - <positive and negative consequences>
Date: YYYY-MM-DD
Author: <name>
```

Sample ADR: Adopt Vertical Slice Architecture (VSA) + Clean Architecture

Title: Adopt Vertical Slice Architecture (VSA) + Clean Architecture
Status: Accepted
Context:
- The repository will host multiple bounded features that may later be extracted into separate services.
- We want minimal coupling between features, clear boundaries, and testable vertical slices.
Decision:
- Use VSA for feature implementation, with Clean Architecture layering (Domain, Application, Infrastructure, API) per module.
- Enforce project dependency rules in `docs/project-dependencies.md` and automated checks in CI.
Consequences:
- + Easier to extract modules to microservices in future.
- + Clear separation of concerns and testability.
- - Slight increase in initial boilerplate for each vertical slice.
Date: 2026-05-20
Author: Team

---

How to add an ADR
1. Copy the template, fill fields, and add to this file.
2. Link the ADR from `docs/onboarding.md` and reference it in PRs that change architectural rules.
