# Backend Feature-First Common Inventory

This inventory tracks backend `Common` areas that should stay cross-cutting versus areas that should keep moving toward feature-first ownership.

## Application

`FoodDiary.Application/Common` should stay limited to technical application primitives:

- `Abstractions/Messaging`: CQRS request and handler marker contracts.
- `Behaviors`: MediatR pipeline behaviors.
- `Models/PagedResponse.cs`: shared paging result model used across application and presentation mappings.
- `Services/PostCommitActionQueue.cs`: cross-cutting post-commit execution queue.
- `Time/UtcDateNormalizer.cs`: shared request date normalization.
- `Validation`: shared low-level enum, user id, and optional entity id parsers.

Feature or domain-purpose helpers should not live under root `Common`. `ManualNutritionLimits` was moved to `FoodDiary.Application/Nutrition/Common` because it is a nutrition-domain policy shared by consumption and recipe features, not a generic application primitive.
`ImageAssetIdParser` was moved to `FoodDiary.Application/Images/Common` because it is an image-domain helper shared by product, recipe, consumption, and user flows.
User preference code parsing lives in `FoodDiary.Application/Users/Common/UserPreferenceCodeParser.cs`; admin-required locale parsing lives in `FoodDiary.Application/Admin/Common/AdminLocaleParser.cs`.

## Application Abstractions

`FoodDiary.Application.Abstractions/Common/Abstractions` remains valid for cross-feature primitives:

- `Audit`: audit logging abstraction.
- `Events`: domain and integration event abstractions.
- `Persistence`: unit-of-work and post-commit queue contracts.
- `Results`: `Result`, `Error`, error kind mapping, and the existing error facade.

Feature-specific repository and service contracts should continue to live under feature folders, usually `Feature/Common`. Existing architecture tests already prevent regrowth of root `Common/Interfaces/Services` and `Common/Interfaces/Persistence`.
Feature-specific error factories should move incrementally to feature folders while preserving the existing `Errors.<Feature>` facade as the compatibility API for existing call sites. Product, recipe, consumption, favorite meal, favorite product, favorite recipe, recipe comment, shopping list, content report, meal plan, daily advice, cycle, cycle day, lesson, image, fasting, wearable, USDA, user, AI, dietologist, admin mail inbox, weight entry, waist entry, hydration entry, and exercise errors now live in feature-owned `Common/*Errors.cs` files, with `Errors.<Feature>` delegating to those feature-owned implementations.

`Errors.Validation`, `Errors.Authentication`, and `Errors.Billing` remain root common categories. They describe cross-cutting result taxonomy rather than a single feature owner, so they should not be forced into feature folders unless the category itself is split into narrower feature-owned errors later.

## Guardrails

- `ApplicationRootCommon_DoesNotRegrowFeatureSpecificNutritionHelpers` prevents `FoodDiary.Application/Common/Nutrition` from returning.
- `ApplicationRootCommon_StaysLimitedToTechnicalApplicationPrimitives` keeps the root `FoodDiary.Application/Common` folder on the approved technical-directory allow-list.
- `ApplicationCommonValidation_StaysLimitedToSharedLowLevelParsers` prevents image and other feature-purpose helpers from returning to root validation.
- `MigratedErrorsFacades_DelegateToFeatureOwnedErrorFactories` requires migrated `Errors.<Feature>` facades to delegate to feature-owned error factories instead of owning inline error codes.
- `ApplicationCommonModels_StayLimitedToSharedApplicationResponsePrimitives` keeps root models limited to `PagedResponse<T>`.
- `ApplicationCommonTime_StaysLimitedToSharedRequestTimeNormalization` keeps root time helpers limited to request UTC normalization.
- `ApplicationAbstractionsCommonPersistenceInterfaces_StayLimitedToCurrentCrossFeatureContracts` prevents root persistence contracts from regrowing.
- `ApplicationCommonServiceInterfaces_StayLimitedToTrueCrossCuttingAbstractions` prevents root service contracts from regrowing.
