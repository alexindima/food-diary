# Architecture Test Guidelines

## Scope
Rules for `tests/FoodDiary.ArchitectureTests/`.

## Role
- Encode architectural decisions as fast guardrail tests.
- Treat these tests as a source of truth for dependency direction, feature structure, source conventions, and service boundaries.

## Current Guardrails
- `ProjectDependencyMatrixTests` owns the production project reference matrix. Add every new production `.csproj` there.
- `LayeringTests` protects primary FoodDiary layer direction.
- `MailRelayArchitectureTests` and `MailInboxArchitectureTests` protect service-specific layer direction and runtime database separation.
- `ApplicationGuardrailTests` protects application-layer conventions and prevents shared buckets from regrowing.
- `AsyncMethodGuardrailTests` uses Roslyn syntax parsing for async suffix and cancellation-token rules.
- `ClientPackageBoundaryTests` protects MailRelay/MailInbox client packages from server-side coupling.
- `HostCompositionBoundaryTests` protects host-only concerns from leaking into application/presentation/resource projects.

## Rules
- Prefer Roslyn-based checks for C# syntax over regex when inspecting declarations.
- Prefer `SourceScanner`, `ProjectReferenceReader`, `ArchitectureTestPaths`, and `CSharpSyntaxReader` over ad-hoc filesystem parsing.
- Keep failures actionable: output relative file paths and line numbers when possible.
- Do not add broad string bans without considering false positives in comments, generated code, or migrations.
- If an architecture boundary changes intentionally, update both the test and the relevant `AGENTS.md`.

## Commands
- Run: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
