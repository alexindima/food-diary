# ADR 0011: Separate the Application Runtime Boundary

- Status: Accepted
- Date: 2026-08-14
- Owners: Backend architecture
- Related: ADR 0004, ADR 0006, ADR 0009
- Supersedes: None

## Context

After application features were extracted into independently compiled projects, the original `FoodDiary.Application` project no longer owned business use cases. It retained mediator pipeline behaviors, post-commit execution, runtime registration, dead shared helpers, and references to many feature projects. Keeping that project as an aggregator obscured the dependency graph and allowed feature dependencies to arrive transitively in executable hosts.

## Decision Drivers

- Business modules must expose and register their own handlers and validators.
- Cross-cutting request execution needs one reusable registration point for every executable host.
- Presentation and infrastructure projects must not depend on an application feature aggregator.
- The business-module inventory must distinguish runtime plumbing from business ownership.

## Considered Options

1. Keep `FoodDiary.Application` as a compatibility facade and feature-project aggregator.
2. Move runtime behavior into each executable host.
3. Replace the legacy project with a dependency-light `FoodDiary.Application.Runtime` project.

## Decision

Use `FoodDiary.Application.Runtime` for mediator pipeline behaviors, the transaction boundary, post-commit action execution, and their service registrations.

The runtime project:

- depends only on `FoodDiary.Application.Abstractions` and `FoodDiary.Mediator`;
- does not reference or scan feature application projects;
- does not contain business handlers, validators, models, or feature services;
- is referenced by executable composition roots, not presentation or infrastructure projects;
- is excluded from the backend business-module inventory.

Executable hosts register `AddApplicationRuntime()` and then explicitly register every feature module they require.

## Consequences

### Positive

- Feature dependencies remain explicit at composition roots.
- Runtime behavior can be reused without importing unrelated business modules.
- The old application aggregator and dead shared helpers can be removed.
- Architecture tests can enforce the final modular boundary directly.

### Negative

- Executable hosts retain an explicit list of feature-module registrations and project references.
- Adding a feature to a host requires updating its composition and dependency guardrails.

## Enforcement

- `ProjectDependencyMatrixTests` permits only abstractions and mediator references from the runtime project.
- `ApplicationGuardrailTests` protects the runtime package and folder surface.
- `BusinessModuleBoundaryTests` prevents feature scanning and registration from returning to runtime.
- Docker dependency tests require every executable image to copy the runtime project explicitly.

## Follow-up

None.
