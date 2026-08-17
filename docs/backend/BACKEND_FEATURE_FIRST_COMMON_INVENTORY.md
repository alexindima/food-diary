# Backend Feature-First Common Inventory

This inventory tracks backend `Common` areas that should stay cross-cutting versus areas that should keep moving toward feature-first ownership.

## Application Runtime and Feature Modules

The legacy root `FoodDiary.Application/Common` area has been removed. Cross-cutting execution code is limited to `FoodDiary.Application.Runtime/Common`:

- `Behaviors`: mediator validation, logging, and command transaction behaviors.
- `Services/PostCommitActionQueue.cs`: bounded post-commit execution.
- `Services/ApplicationRuntimeTelemetry.cs`: runtime pipeline telemetry.

CQRS contracts, persistence markers, shared result shapes, and other adapter-facing contracts live in `FoodDiary.Application.Abstractions`. Feature-purpose helpers live in the owning `FoodDiary.Application.<Feature>` project. For example, image parsing lives in `FoodDiary.Application.Images/Common`, and user preference parsing lives in `FoodDiary.Application.Users/Common`.

## Application Abstractions

`FoodDiary.Application.Abstractions/Common/Abstractions` remains valid for cross-feature primitives:

- `Audit`: audit logging abstraction.
- `Events`: domain and integration event abstractions.
- `Persistence`: unit-of-work and post-commit queue contracts.
- `Results`: `Result`, `Error`, error kind mapping, and the existing error facade.

Feature-specific repository and service contracts should continue to live under feature folders, usually `Feature/Common`. Existing architecture tests already prevent regrowth of root `Common/Interfaces/Services` and `Common/Interfaces/Persistence`.
Feature-specific error factories should move incrementally to feature folders while preserving the existing `Errors.<Feature>` facade as the compatibility API for existing call sites. Product, recipe, meal, favorite meal, favorite product, favorite recipe, recipe comment, shopping list, content report, meal plan, daily advice, cycle, cycle day, lesson, image, fasting, wearable, USDA, user, AI, dietologist, admin mail inbox, weight entry, waist entry, hydration entry, and exercise errors now live in feature-owned `Common/*Errors.cs` files, with `Errors.<Feature>` delegating to those feature-owned implementations.

`Errors.Validation`, `Errors.Authentication`, and `Errors.Billing` remain root common categories. They describe cross-cutting result taxonomy rather than a single feature owner, so they should not be forced into feature folders unless the category itself is split into narrower feature-owned errors later.

## Guardrails

- `ApplicationRootCommon_DoesNotRegrowFeatureSpecificNutritionHelpers` prevents the removed root application common area from returning.
- Runtime-project guardrails keep `FoodDiary.Application.Runtime/Common` limited to technical execution behavior.
- Feature-structure tests keep feature-purpose helpers inside their owning application module.
- `ApplicationAbstractionsErrorsRoot_ContainsOnlyCommonTaxonomyOrMigratedFacades` prevents new root error catalogs from appearing without an explicit common-taxonomy or migrated-facade decision.
- `MigratedErrorsFacades_DelegateToFeatureOwnedErrorFactories` requires migrated `Errors.<Feature>` facades to delegate to feature-owned error factories instead of owning inline error codes.
- `ApplicationAbstractionsCommonPersistenceInterfaces_StayLimitedToCurrentCrossFeatureContracts` prevents root persistence contracts from regrowing.
