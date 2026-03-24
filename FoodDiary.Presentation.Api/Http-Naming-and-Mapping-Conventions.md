# HTTP Naming and Mapping Conventions

## Rule

Presentation models must be explicit about their transport role.

Use:

- `*HttpRequest` for request bodies
- `*HttpQuery` for grouped query parameters
- `*HttpMappings` for mapping from presentation models to application commands/queries

## Examples

- `CreateWaistEntryHttpRequest`
- `UpdateUserHttpRequest`
- `GetProductsHttpQuery`
- `UserHttpMappings`

## Controller Flow

Each endpoint should follow this shape:

1. accept a presentation model from `FoodDiary.Presentation.Api`
2. resolve route/query/current-user context
3. map to an application `Command` or `Query`
4. call `Mediator.Send(...)`

Target flow:

- `Controller -> HttpRequest/HttpQuery -> HttpMappings -> Command/Query -> MediatR`

## Layer Rule

If a mapping starts from an incoming HTTP/contract request model and produces a MediatR command/query, it belongs to `FoodDiary.Presentation.Api`.

It does not belong to `FoodDiary.Application`.

## Contracts Rule

Do not add new HTTP request models to `FoodDiary.Contracts`.

`FoodDiary.Contracts` should gradually keep only:

- stable shared response models
- genuinely shared framework-agnostic contracts

## Migration Rule

Migrate feature-by-feature.

For each controller:

- move incoming request DTOs into `Presentation.Api/Features/<Feature>/Requests`
- add `Http` suffix
- move request-to-command/query mappings into `Presentation.Api/Features/<Feature>/Mappings`
- update controller to use only presentation request models
