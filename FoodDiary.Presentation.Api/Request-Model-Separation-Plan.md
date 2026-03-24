# Request Model Separation Plan

## Goal

Separate transport-level HTTP models from application-level requests.

Target state:

- HTTP request models live in `FoodDiary.Presentation.Api`
- MediatR commands and queries live in `FoodDiary.Application`
- mapping from HTTP models to application requests happens in `FoodDiary.Presentation.Api`
- `FoodDiary.Contracts` stops being a bucket for ASP.NET request DTOs

## Current Situation

Right now the boundaries are mixed:

- controllers in `FoodDiary.Presentation.Api` still accept many request models from `FoodDiary.Contracts`
- `FoodDiary.Application` already contains mapping classes such as:
  - `ConsumptionRequestMappings`
  - `CycleRequestMappings`
  - `HydrationRequestMappings`
  - `ShoppingListRequestMappings`
  - `WaistEntryRequestMappings`
  - `WeightEntryRequestMappings`
- `FoodDiary.Contracts` contains many request types that are actually HTTP transport models:
  - `RegisterRequest`
  - `LoginRequest`
  - `CreateProductRequest`
  - `UpdateProductRequest`
  - `CreateRecipeRequest`
  - `UpdateRecipeRequest`
  - `CreateHydrationEntryRequest`
  - and many others

This means:

- presentation concerns leak into `Contracts`
- application carries HTTP mapping concerns
- controller input shape and application request shape are coupled more than necessary

## Architectural Decision

Use three distinct categories.

### 1. Presentation Models

Live in:

- `FoodDiary.Presentation.Api`

Purpose:

- represent HTTP request and response shapes
- carry transport semantics such as body/query/route-oriented fields
- support versioning and API-specific formatting without affecting application request types

Examples:

- `CreateProductHttpRequest`
- `UpdateRecipeHttpRequest`
- `LoginHttpRequest`
- `GetStatisticsHttpQuery`

### 2. Application Requests

Live in:

- `FoodDiary.Application`

Purpose:

- represent use cases
- be consumed by MediatR handlers
- stay independent from ASP.NET transport concerns

Examples:

- `CreateProductCommand`
- `UpdateRecipeCommand`
- `LoginCommand`
- `GetStatisticsQuery`

### 3. Shared Contracts

Live in:

- `FoodDiary.Contracts`

Purpose:

- hold only genuinely shared, stable, framework-agnostic contracts
- preferably response DTOs or integration-facing models that are reused across boundaries

Examples that may stay:

- response DTOs returned by application/use cases if they are intentionally stable
- common wrappers like `PagedResponse<T>`
- DTOs reused by more than one adapter or external consumer

Examples that should move out:

- body/query request DTOs used only by ASP.NET controllers

## Design Rules

### Rule 1. Controllers do not send transport models to MediatR

Controllers should:

- receive a presentation request model
- resolve route/query/user context
- map to an application command/query
- call `Mediator.Send(applicationRequest)`

### Rule 2. Application does not contain HTTP request mapping classes

Mapping classes like:

- `*RequestMappings.cs`

should move out of `FoodDiary.Application` and into `FoodDiary.Presentation.Api`.

Reason:

- mapping from HTTP request model to command/query is presentation work, not application work

### Rule 3. Application request types belong to feature folders

Application request models stay where they already belong:

- `Feature/Commands/<Action>/`
- `Feature/Queries/<Action>/`

They should remain the only request objects handled by MediatR handlers.

### Rule 4. `Contracts` should be conservative

A type goes into `FoodDiary.Contracts` only if it is:

- genuinely shared across boundaries
- intended to stay stable
- not tied to ASP.NET transport details

If the type is only used by a controller action input, it should not live in `Contracts`.

## Recommended Folder Structure

### In `FoodDiary.Presentation.Api`

For each feature:

- `Features/<Feature>/Requests/`
- `Features/<Feature>/Responses/` if needed
- `Features/<Feature>/Mappings/`
- `Features/<Feature>/<Feature>Controller.cs`

Examples:

- `Features/Products/Requests/CreateProductHttpRequest.cs`
- `Features/Products/Requests/UpdateProductHttpRequest.cs`
- `Features/Products/Mappings/ProductHttpMappings.cs`

### In `FoodDiary.Application`

Keep:

- commands
- queries
- handlers
- validators
- application output models if they are use-case DTOs

Remove over time:

- request mapping classes that depend on controller/request DTO shape

## Naming Recommendation

To make boundaries obvious, use explicit names for presentation input models.

Recommended suffixes:

- `HttpRequest`
- `HttpQuery`
- `HttpBody`

Examples:

- `RegisterHttpRequest`
- `UpdateUserHttpRequest`
- `GetProductsHttpQuery`

This is clearer than reusing generic names like `CreateProductRequest`, which tend to become ambiguous once application requests also exist.

## What To Do With Responses

Responses can be migrated more slowly than requests.

Recommended rule:

- first separate request models
- keep existing response DTOs stable until request separation is finished

Then decide feature by feature:

- keep shared response DTOs in `Contracts` if they are stable and reused
- move HTTP-specific response models into `Presentation.Api` if they are transport-shaped
- keep application output DTOs in `Application` only if they are truly use-case-facing and not shared externally

## Migration Strategy

Do not do this as a single huge rewrite.

Use a feature-by-feature migration.

### Phase 1. Freeze the Target Rule

From now on:

- no new HTTP request DTOs go into `FoodDiary.Contracts`
- no new HTTP request mapping classes go into `FoodDiary.Application`

New work should use the target structure immediately.

### Phase 2. Introduce Presentation Request Folders

For each feature in `FoodDiary.Presentation.Api`, create:

- `Requests/`
- `Mappings/`

Do this even before moving all models, so the target structure is visible.

### Phase 3. Migrate the Simple Features First

Best first candidates:

- Hydration
- WeightEntries
- WaistEntries
- Goals
- Statistics

Why:

- they usually have fewer nested request objects
- they already have very direct request-to-command mappings

For each feature:

1. create new HTTP request model(s) in `Presentation.Api`
2. move request mapping from `Application/*/Mappings` to `Presentation.Api`
3. update controller to use the new model
4. stop referencing old request DTOs from `Contracts`

### Phase 4. Migrate Medium-Complexity Features

Next candidates:

- Products
- Recipes
- ShoppingLists
- Consumptions
- Cycles

These have nested request models and more mapping logic, so they should come after the simple features.

### Phase 5. Migrate Auth Separately

Auth deserves its own pass because:

- request models are externally sensitive
- some payloads are likely consumed directly by clients
- there may be more compatibility risk

Suggested auth pass:

- introduce `Presentation.Api` HTTP request models first
- keep property names and JSON shape identical
- only then switch controllers over

### Phase 6. Clean Up `Contracts`

After enough features are migrated, classify every type in `FoodDiary.Contracts` into:

- keep in `Contracts`
- move to `Presentation.Api`
- move to `Application`

Do not start with this step.

Start with usage-driven migration, then clean up the leftovers once the direction is established.

## Candidate Features to Migrate First

### Wave 1

- `Hydration`
- `WeightEntries`
- `WaistEntries`
- `Goals`
- `Statistics`

### Wave 2

- `Products`
- `Recipes`
- `ShoppingLists`
- `Cycles`
- `Consumptions`

### Wave 3

- `Authentication`
- `Admin`
- `Dashboard`
- `Ai`

Dashboard and AI can wait because they involve more orchestration and more mixed response shapes.

## How Controllers Should Look After Migration

Controller flow should become:

1. receive `HttpRequest` / `HttpQuery`
2. read route/query/user context
3. map to `Command` / `Query`
4. call `Mediator.Send(...)`
5. map output only if needed

That is enough for most endpoints.

Do not add a separate presentation service unless it actually adds value.

## When a Presentation Service Is Justified

Create a presentation-level facade/service only when there is real orchestration such as:

- combining multiple application requests
- reusable controller logic across multiple endpoints
- transport-specific branching or normalization that is too large for the controller

Do not create a service that only wraps:

- map request
- call mediator once

That adds ceremony without improving the boundary.

## Backward Compatibility Rule

When replacing request DTO classes:

- keep the JSON payload shape identical unless you intentionally version the API
- do not change property names casually
- do not change nullability/optional semantics without checking client impact

The physical class can move while the HTTP contract stays the same.

## Risks

### Risk 1. Partial split creates duplicate concepts

You may temporarily have:

- old request DTO in `Contracts`
- new request DTO in `Presentation.Api`

This is acceptable during migration, but only briefly and feature by feature.

### Risk 2. Application still depends on transport shape

If `Application` keeps the old `*RequestMappings.cs`, the split is only cosmetic.

Mitigation:

- move mapping out of `Application` in the same feature migration

### Risk 3. `Contracts` remains a mixed bag

If no cleanup policy is enforced, the old pattern will continue.

Mitigation:

- establish the no-new-request-models-in-contracts rule immediately

## Concrete End State

Best end state:

- `Presentation.Api` owns HTTP request models and request mapping
- `Application` owns commands, queries, handlers, validators
- `Contracts` contains only truly shared, stable contracts

This gives cleaner layering and makes the `Presentation.Api` extraction actually meaningful.

## Recommended Next Step

Implement one pilot migration first.

Best pilot:

- `Hydration`

Reason:

- small surface
- straightforward request mapping
- low risk

After one successful pilot, reuse the same pattern for the rest of the features.
