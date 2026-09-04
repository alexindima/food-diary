# ADR 0028: Retire the Central Application Abstractions Aggregator

- Status: Accepted
- Date: 2026-09-05
- Owners: Backend architecture
- Related: ADR 0004, ADR 0011, ADR 0027
- Supersedes: ADR 0004

## Context

After feature contracts moved into module-owned `Application/Abstractions`, `Contracts`, and `Domain.Contracts` projects, `FoodDiary.Application.Abstractions` no longer represented one cohesive boundary. It mixed generic request and transaction contracts with audit, authentication, email, nutrition, outbox administration, and two Users helpers. Most consumers referenced the aggregate project for only one or two concerns, hiding their real dependency.

## Decision

Remove the central project and split its remaining types by stable ownership:

- generic command/query, event, transaction, error-taxonomy, pagination, temporal, and enum-validation contracts: `Shared/FoodDiary.Application.Contracts`;
- audit contracts: `Shared/FoodDiary.Audit.Contracts`;
- short-lived authentication store contract: `Shared/FoodDiary.Authentication.Contracts`;
- email delivery/outbox contracts and telemetry: `Shared/FoodDiary.Email.Contracts`;
- shared manual-nutrition limits: `Shared/FoodDiary.Nutrition.Contracts`;
- dead-letter replay administration: `Shared/FoodDiary.Outbox.Management.Contracts`;
- current-user resolution and user-id parsing: `Modules/Users/Contracts`.

Preserve existing CLR namespaces during the move so this is a physical dependency-boundary change, not a source-level API rename. Consumers must reference the actual owner directly. `Shared/FoodDiary.Outbox.Abstractions` remains limited to the record marker used by persistence models and must not absorb management services.

## Consequences

- Project references now reveal the technical or module capability each consumer actually uses.
- No shared project aggregates module-specific feature contracts.
- Some projects gain several small direct references in place of one broad reference.
- A coordinated rebuild is required because the assembly containing preserved CLR types changes.

## Enforcement

- `SharedApplicationContractsBoundaryTests` protects the generic package.
- `UsersIdentityContractOwnershipTests` protects the two Users/authentication seam owners.
- `ProjectDependencyMatrixTests` records every direct dependency.
- Docker and solution guardrails require the new projects and reject the retired project/lock file.
