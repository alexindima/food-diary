# Backend Feature-First Common Inventory

This inventory tracks backend `Common` areas that should stay cross-cutting versus areas that should keep moving toward feature-first ownership.

## Application

`FoodDiary.Application/Common` should stay limited to technical application primitives:

- `Abstractions/Messaging`: CQRS request and handler marker contracts.
- `Behaviors`: MediatR pipeline behaviors.
- `Models/PagedResponse.cs`: shared paging result model used across application and presentation mappings.
- `Services/PostCommitActionQueue.cs`: cross-cutting post-commit execution queue.
- `Time/UtcDateNormalizer.cs`: shared request date normalization.
- `Validation`: shared low-level parsers and value-object validation helpers.

Feature or domain-purpose helpers should not live under root `Common`. `ManualNutritionLimits` was moved to `FoodDiary.Application/Nutrition/Common` because it is a nutrition-domain policy shared by consumption and recipe features, not a generic application primitive.

## Application Abstractions

`FoodDiary.Application.Abstractions/Common/Abstractions` remains valid for cross-feature primitives:

- `Audit`: audit logging abstraction.
- `Events`: domain and integration event abstractions.
- `Persistence`: unit-of-work and post-commit queue contracts.
- `Results`: `Result`, `Error`, error kind mapping, and the existing error facade.

Feature-specific repository and service contracts should continue to live under feature folders, usually `Feature/Common`. Existing architecture tests already prevent regrowth of root `Common/Interfaces/Services` and `Common/Interfaces/Persistence`.

## Guardrails

- `ApplicationRootCommon_DoesNotRegrowFeatureSpecificNutritionHelpers` prevents `FoodDiary.Application/Common/Nutrition` from returning.
- `ApplicationAbstractionsCommonPersistenceInterfaces_StayLimitedToCurrentCrossFeatureContracts` prevents root persistence contracts from regrowing.
- `ApplicationCommonServiceInterfaces_StayLimitedToTrueCrossCuttingAbstractions` prevents root service contracts from regrowing.
