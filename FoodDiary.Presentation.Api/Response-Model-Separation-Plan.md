# Response Model Separation Plan

## Goal

Separate application output models from HTTP response models without breaking the current API shape.

Target state:

- `FoodDiary.Application` returns application output models
- `FoodDiary.Presentation.Api` maps application output models to HTTP response models
- `FoodDiary.Contracts` keeps only truly shared contracts that are intentionally stable across boundaries

## Current Situation

Request-side separation is already done:

- HTTP request models live in `FoodDiary.Presentation.Api`
- commands and queries live in `FoodDiary.Application`
- request mappings live in `FoodDiary.Presentation.Api`

Response-side is still mixed:

- many handlers return `FoodDiary.Contracts.*` DTOs directly
- many `Application/*/Mappings/*Mappings.cs` files create `Contracts` response DTOs
- controllers often return `Result<T>` where `T` is still a `Contracts` type

Examples:

- `HydrationEntryResponse`
- `WeightEntryResponse`
- `WaistEntryResponse`
- `ProductResponse`
- `RecipeResponse`
- `AuthenticationResponse`
- `DashboardSnapshotResponse`

This means the application layer still knows too much about the HTTP-facing response shape.

## Architectural Decision

Use three categories.

### 1. Application Output Models

Live in:

- `FoodDiary.Application/<Feature>/Models/`
- or `FoodDiary.Application/<Feature>/Responses/` if the team prefers that name

Purpose:

- represent use-case output
- be returned by handlers
- stay independent from ASP.NET concerns and HTTP-specific transport decisions

Examples:

- `HydrationEntryModel`
- `HydrationDailyModel`
- `ProductModel`
- `PagedProductsModel`

### 2. Presentation Response Models

Live in:

- `FoodDiary.Presentation.Api/Features/<Feature>/Responses/`

Purpose:

- represent HTTP response payloads
- preserve API-specific naming and shape
- allow response versioning without forcing application changes

Examples:

- `HydrationEntryHttpResponse`
- `HydrationDailyHttpResponse`
- `ProductHttpResponse`

### 3. Shared Contracts

Live in:

- `FoodDiary.Contracts`

Purpose:

- hold only genuinely shared contracts
- remain stable and version-friendly
- be reused across adapters or external consumers only when that is intentional

Likely keep:

- `PagedResponse<T>`
- AI DTOs used by multiple adapters or services
- contracts reused by Telegram bot or other non-HTTP boundaries

Likely move out over time:

- feature response DTOs used only by API controllers

## Important Rule

Do not migrate all response models at once.

Response DTOs are more sensitive than request DTOs because they affect:

- API clients
- tests
- swagger shape
- frontend expectations

So the migration should preserve JSON payload shape while changing only the internal ownership of the types.

## Recommended End State

Controller flow:

1. receive `HttpRequest` / `HttpQuery`
2. map to command/query
3. `Mediator.Send(...)`
4. receive application output model
5. map application output model to `HttpResponse`
6. return HTTP payload

This gives a clean split:

- application owns use-case outputs
- presentation owns HTTP outputs

## Migration Strategy

### Phase 1. Freeze the Rule

From now on:

- no new feature response DTOs should be introduced in `FoodDiary.Contracts` unless they are truly shared
- new handlers should prefer application output models over `Contracts` response types

### Phase 2. Pilot on Simple Features

Best pilot candidates:

- `Hydration`
- `WeightEntries`
- `WaistEntries`

Why:

- small response surface
- simple mappings
- low client-shape complexity

For each pilot feature:

1. add application output model(s)
2. change command/query return types to use application output models
3. change application mappings from `ToResponse()` to `ToModel()` or similar
4. add presentation response models
5. add presentation response mappings
6. map result value in controller before returning
7. keep JSON shape identical

### Phase 3. Medium Features

Next wave:

- `Users`
- `Goals`
- `Products`
- `Recipes`
- `ShoppingLists`
- `Consumptions`

These have richer payloads, nested structures, or page wrappers.

### Phase 4. High-Impact Features

Last wave:

- `Authentication`
- `Dashboard`
- `Admin`
- `AI`

These are more sensitive because:

- some are likely consumed directly by multiple clients
- some aggregate many nested response types
- some may still belong partly in `Contracts`

## Mapping Rules

### Rule 1. Application mappings should not build HTTP response models

Inside `FoodDiary.Application`, mappings should create application output models only.

Bad:

- `Product -> ProductResponse` where `ProductResponse` is HTTP-facing

Good:

- `Product -> ProductModel`

### Rule 2. Presentation maps app output to HTTP output

Inside `FoodDiary.Presentation.Api`, add:

- `Features/<Feature>/Mappings/<Feature>HttpResponseMappings.cs`

Examples:

- `HydrationHttpResponseMappings`
- `ProductHttpResponseMappings`

### Rule 3. Preserve HTTP payload shape

When introducing `*HttpResponse`, keep:

- property names
- nullability semantics
- list nesting
- object structure

The class ownership changes first. The API contract should not.

## What To Do With Contracts

Do not try to empty `FoodDiary.Contracts` immediately.

Use this rule:

- if the type is only used by API handlers/controllers, it should eventually move out
- if the type is intentionally shared by several adapters or external consumers, it may stay

Practical intermediate state is acceptable:

- some response models still in `Contracts`
- new pilot features already use separate application + presentation models

## Pilot Recommendation

Start with `Hydration`.

Desired target:

- `FoodDiary.Application/Hydration/Models/HydrationEntryModel.cs`
- `FoodDiary.Application/Hydration/Models/HydrationDailyModel.cs`
- `FoodDiary.Presentation.Api/Features/Hydration/Responses/HydrationEntryHttpResponse.cs`
- `FoodDiary.Presentation.Api/Features/Hydration/Responses/HydrationDailyHttpResponse.cs`
- `FoodDiary.Presentation.Api/Features/Hydration/Mappings/HydrationHttpResponseMappings.cs`

Then:

- handlers return application models
- controller maps to HTTP responses before `Ok(...)`

## Risks

### Risk 1. Double DTO Explosion

If every feature gets both application and presentation DTOs without discipline, the repo will bloat.

Mitigation:

- start only where the separation clearly helps
- use pilot waves
- keep names consistent

### Risk 2. Breaking API shape accidentally

Mitigation:

- copy current response property shape exactly
- migrate one feature at a time

### Risk 3. Mixed result-handling helpers

`ResultExtensions.ToActionResult<T>()` currently returns `OkObjectResult(result.Value)` directly.

Once controllers start mapping output models, some endpoints may need:

- manual success handling
- or a second helper that maps `Result<TApp>` to `ActionResult`

Recommended first step:

- do manual success mapping in pilot controllers
- only generalize helper methods after one or two pilots

## First Execution Step

Implement the pilot on `Hydration` first.

After that, repeat the exact pattern on:

- `WeightEntries`
- `WaistEntries`

Those three features will establish the response-side convention for the rest of the solution.
