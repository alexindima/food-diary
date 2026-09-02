# Tooling Test Guidelines

Rules for `Tooling/tests/`. Follow `tests/AGENTS.md` for cross-repository test rules.

- Own focused tests for FoodDiary.Analyzers and FoodDiary.Development.Mcp here.
- Keep projects under `/Tooling/tests/` in `FoodDiary.slnx`.
- Production tool projects retain their root paths; do not change analyzer wiring or MCP runtime behavior during test relocation.
- Reuse `tests/Directory.Build.props`, runsettings and runner configuration; preserve project and assembly names.
- Keep repository architecture guardrails in `tests/FoodDiary.ArchitectureTests`.

Run `dotnet test Tooling/tests/<Project>/<Project>.csproj` with repository-level `--artifacts-path`.
