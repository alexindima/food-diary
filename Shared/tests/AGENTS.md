# Shared Test Guidelines

Rules for `Shared/tests/`. Follow `tests/AGENTS.md` for cross-repository test rules.

- Own focused tests for Domain.Primitives, Mediator and Results here.
- Keep projects under `/Shared/tests/` in `FoodDiary.slnx`.
- Reuse `tests/Directory.Build.props`, runsettings and runner configuration; do not duplicate them.
- Preserve project/assembly names and each test project's direct dependency on its owner.
- Cross-module, host and shared test-support projects remain under their existing owners.

Run `dotnet test Shared/tests/<Project>/<Project>.csproj` with repository-level `--artifacts-path`.
