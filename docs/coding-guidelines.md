# Coding guidelines

Principles
- Small, focused classes and methods. Prefer composition over inheritance.
- Constructor injection for dependencies; avoid service locator.
- Use `IOptions<T>` for configuration POCOs.
- Prefer async APIs with cancellation tokens passed through.

Best practices
- Keep methods ideally under ~40 lines.
- Avoid static mutable state.
- Map DTOs explicitly to/from domain models at boundaries.
