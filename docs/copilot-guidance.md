# Guidance for Copilot and Automated Assistants

- Always respect VSA + Clean Architecture rules in `docs/architecture.md`.
- Include unit tests (xUnit + Moq + Shouldly) for any suggested logic changes.
- Do not introduce project-level dependencies that break the project-dependency rules.
- Prefer small, incremental changes and include migration steps for structural changes.
