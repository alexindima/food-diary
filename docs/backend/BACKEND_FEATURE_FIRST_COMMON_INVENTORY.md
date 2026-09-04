# Backend Feature-First Common Inventory

This inventory tracks backend `Common` areas that should stay cross-cutting versus areas that should keep moving toward feature-first ownership.

## Application Runtime and Feature Modules

The legacy root `FoodDiary.Application/Common` area has been removed. Cross-cutting execution code is limited to `FoodDiary.Application.Runtime/Common`:

- `Behaviors`: mediator validation, logging, and command transaction behaviors.
- `Services/PostCommitActionQueue.cs`: bounded post-commit execution.
- `Services/ApplicationRuntimeTelemetry.cs`: runtime pipeline telemetry.

Generic CQRS contracts, transaction markers, result taxonomy, pagination, and validation primitives live in `Shared/FoodDiary.Application.Contracts`. Audit, authentication, email, nutrition, and outbox-management contracts have their own narrow projects under `Shared/`. Feature-purpose helpers and ports live with their owning module.

## Shared contract projects

`Shared/FoodDiary.Application.Contracts/Common` is limited to generic application primitives:

- `Events`: domain and integration event abstractions.
- `Persistence`: unit-of-work and post-commit queue contracts.
- `Results`: common error taxonomy and error-kind mapping; `Result` itself remains in `Shared/FoodDiary.Results`.

Feature-specific repository, service, projection, and error contracts live in their module's `Application/Abstractions` or `Contracts` project. Architecture tests prevent `FoodDiary.Application.Contracts` from regrowing feature folders or implementation dependencies.

Only the generic validation/authentication taxonomy remains in the shared application package. Billing and every other feature-specific factory are module-owned.

## Guardrails

- `ApplicationRootCommon_DoesNotRegrowFeatureSpecificNutritionHelpers` prevents the removed root application common area from returning.
- Runtime-project guardrails keep `FoodDiary.Application.Runtime/Common` limited to technical execution behavior.
- Feature-structure tests keep feature-purpose helpers inside their owning application module.
- `SharedApplicationContractsBoundaryTests` keeps the generic shared package dependency-light and free of feature folders.
- `RetiredErrorFacadeTests` prevents feature error facades from returning to the shared package.
- `ProjectDependencyMatrixTests` records every direct owner/shared-contract dependency.
