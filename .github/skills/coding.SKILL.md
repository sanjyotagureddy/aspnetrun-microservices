# Coding (skill)

Purpose: condensed, actionable coding rules for automated assistants and linters.

Core principles
- Follow SOLID and favor composition over inheritance.
- Prefer constructor injection and explicit dependencies; avoid service-locator and static mutable state.

Asynchronous and cancellation
- All public async APIs must accept `CancellationToken` and propagate it to downstream calls.

Error handling
- Do not swallow exceptions; map domain exceptions to typed error responses at API boundaries.

DTOs and mapping
- Keep `Domain` models free of framework concerns. Map to/from DTOs explicitly in `Application` layer.
- Use mapping libraries (AutoMapper) only through configuration profiles in `Application` and keep mapping code testable.

Module boundaries and reusability
- Design classes to be module-scoped; minimize internal coupling. Prefer passing interfaces in constructors.
- Avoid exposing framework-specific types (e.g., DbContext, HttpContext) across module boundaries.

Code quality
- Write small methods; prefer < 40 lines where reasonable.
- Add XML doc comments for public types and important behavior.

