# Contracts Removal Plan

## Current State

`FoodDiary.Contracts` is no longer acting as a true shared boundary contract project.

After the request/response separation work:

- `Application` no longer depends on feature DTOs from `Contracts`
- `Presentation.Api` no longer depends on feature DTOs from `Contracts`
- all former production usages have already been replaced with:
  - `Application.Common.Models.PagedResponse<T>`
  - presentation-level `*HttpResponse` types

Remaining cleanup is structural:

- remove `FoodDiary.Contracts` project references
- remove the project from the solution
- delete the obsolete DTO source files

## Removal Order

1. Move request/response DTO ownership to `Presentation.Api`.
2. Move application output models to `Application`.
3. Replace shared `PagedResponse<T>` with `Application.Common.Models.PagedResponse<T>`.
4. Replace feature-specific `Contracts.*` usages in tests.
5. Remove `FoodDiary.Contracts` project references.
6. Remove `FoodDiary.Contracts.csproj` from the solution.
7. Delete the `FoodDiary.Contracts` project.

## Why The Project Can Go Away

- HTTP contracts now belong to the ASP.NET adapter, not to a shared DTO bucket.
- Application use-case outputs now belong to `Application`.
- `Contracts` no longer represents a real cross-process boundary.
- Keeping it would only preserve an extra project with no architectural owner.

## If Shared Contracts Are Needed Again

Introduce a new dedicated contract assembly only for a real external boundary, for example:

- public SDK models
- messaging/integration events
- generated client contracts

Do not reuse a generic `Contracts` project as a catch-all DTO folder again.
