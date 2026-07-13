# ADR 0009: Executable Application Module Dependency Graph

- Status: Accepted
- Date: 2026-07-13
- Owners: Backend architecture
- Related: ADR-0001, ADR-0004, ADR-0006
- Supersedes: None

## Context

ADR-0006 established business ownership without requiring one assembly per module. As more modules adopted semantic APIs, their allowed in-process dependencies became difficult to infer from project references alone because most implementation still lives in `FoodDiary.Application`.

Source-level conventions without an explicit graph allow accidental dependencies and cycles to accumulate. Promoting every module to a separate assembly would make dependencies visible to the compiler, but would impose substantial project, registration, and migration overhead before independent deployment or reuse is needed.

## Decision Drivers

- New cross-module dependencies must be visible in review and CI.
- The Application module graph should remain acyclic.
- Logical ownership should not require premature assembly or service extraction.
- The current allowed graph needs one canonical, machine-readable representation.

## Considered Options

1. Rely on code review and documentation only. This has low implementation cost but cannot reliably prevent drift.
2. Extract every business module into a separate CLR assembly. This gives compiler-enforced references but adds excessive physical structure and migration cost.
3. Keep modules in the current assemblies and enforce an explicit source-derived dependency graph with architecture tests.

## Decision

- Keep the canonical direct Application module graph in `docs/architecture/module-dependencies.json`.
- Derive actual Module API dependencies from source and require them to match the manifest.
- Reject unknown modules, self-edges, unacknowledged dependencies, and cycles.
- Keep `knownCycles` empty. Any future exception requires a new architectural decision rather than a silent manifest update.
- Promote a module to a separate assembly only when runtime, ownership, reuse, build isolation, or another concrete pressure justifies it.

## Consequences

### Positive

- Logical module boundaries are executable without requiring assembly-per-module architecture.
- Dependency additions and cycle risks appear explicitly in review.
- The manifest provides a compact current view of Application coupling.

### Negative

- Source analysis depends on naming and Module API conventions.
- Legitimate new collaboration requires coordinated code, manifest, documentation, and test changes.
- CLR visibility does not enforce most module boundaries; architecture tests remain part of the trusted enforcement mechanism.

## Enforcement

- `docs/architecture/module-dependencies.json`
- `tests/FoodDiary.ArchitectureTests/ModuleDependencyGraphTests.cs`
- `tests/FoodDiary.ArchitectureTests/BusinessModuleBoundaryTests.cs`

## Follow-up

- Keep the current module inventory and interaction rules in `docs/backend/BACKEND_MODULE_OWNERSHIP.md`.
