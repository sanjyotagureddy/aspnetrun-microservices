# CI and enforcement

Minimum CI steps
- `dotnet restore` → `dotnet build` → `dotnet test` (collect coverage) → generate coverage report → fail if < 90%.
- Run `dotnet format --verify-no-changes` and analyzers; treat defined warnings as errors.

Security
- Run CodeQL / SCA and Dependabot. Block secrets via pre-commit or CI scanner.

Automation
- Add GitHub Actions for format, build/test/coverage, and secret scanning. Expose artifacts for coverage reports.
