# Repository Instruction Documents

This folder contains the canonical, repo-level instruction documents for automated assistants and contributors. These files are the source of truth for guidance used by Copilot/agents and CI checks.

Files
- `architecture.instructions.md` — architecture rules and e‑commerce patterns.
- `coding.instructions.md` — coding conventions, visibility rules, and best practices.
- `design-patterns.instructions.md` — recommended patterns (MediatR, Outbox, Sagas, etc.).
- `testing.instructions.md` — testing strategy, coverage targets, and CI commands.
- `project-dependencies.instructions.md` — allowed project dependency rules and enforcement guidance.
- `ci.instructions.md` — CI pipeline stages, artifacts and enforcement points.

Usage
- Automated assistants should read these files before making changes that affect architecture, CI, testing, or project structure.
- Human contributors should consult these documents during design and PR creation.

Maintenance
- Keep these docs in sync with `docs/` and propose ADRs in `docs/architectural-decisions.md` for major changes.
- When updating an instruction file, also update this README briefly to reflect any structural changes.
