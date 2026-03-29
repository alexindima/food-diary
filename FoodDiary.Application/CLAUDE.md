# FoodDiary.Application

Application layer implementing CQRS via MediatR with FluentValidation pipeline.

## Architecture

### Feature-Slice Organization
Each domain concept is a top-level folder with self-contained structure:
```
{Feature}/
  Commands/{UseCaseName}/   — Command record + Validator + Handler
  Queries/{UseCaseName}/    — Query record + Validator + Handler
  Common/                   — Repository interface, shared DTOs
  Mappings/                 — Static extension methods (entity -> model)
  Models/                   — Application-layer DTOs
  Services/                 — Feature-specific application services
```

### CQRS Abstractions (`Common/Abstractions/Messaging/`)
- `ICommand<TResponse>` / `IQuery<TResponse>` — thin wrappers over MediatR `IRequest<T>`
- `ICommandHandler<T, TResponse>` / `IQueryHandler<T, TResponse>` — thin wrappers over `IRequestHandler`
- `IUserRequest` — marker interface with `Guid? UserId` for user-scoped operations

### Result Pattern (`Common/Abstractions/Result/`)
All handlers return `Result` or `Result<T>`, never throw for business errors.
- `Result.Success()` / `Result.Success<T>(value)` / `Result.Failure(error)` / `Result.Failure<T>(error)`
- `Error` — sealed record with `Code` (dot-notation like `"Product.NotFound"`), `Message`, optional `Details`, and `Kind` (ErrorKind enum)
- `Errors` class — catalog of all domain error factories organized by nested static classes
- `ErrorKindResolver` — maps error codes to `ErrorKind` for HTTP status resolution

### Pipeline Behaviors (registered in order)
1. `LoggingBehavior` — wraps every request in Stopwatch, logs warnings on failure
2. `ValidationBehavior` — runs all FluentValidation validators in parallel, short-circuits with `Result.Failure` on validation errors

## Key Patterns

### Command/Query/Handler Convention
Each use case folder contains up to 3 files:
1. **Record** — `record {Name}(...) : ICommand<Result<T>>` or `IQuery<Result<T>>`
2. **Validator** — `{Name}Validator : AbstractValidator<{Name}>` (not all have one)
3. **Handler** — `{Name}Handler : ICommandHandler<{Name}, Result<T>>` with business logic

### Validator Conventions
- Every rule sets `.WithErrorCode()` matching codes from `Errors` catalog
- Every rule sets `.WithMessage()` with human-readable text
- Validators can inject repositories for async validation (`MustAsync`)
- `CascadeMode.Stop` when early exit is desired
- Auto-registered via `AddValidatorsFromAssembly(assembly, includeInternalTypes: true)`

### Handler Conventions
- Newer handlers use C# primary constructors; older ones use traditional constructor injection
- Return early on failure using `Errors` catalog
- Entity-to-model mapping via extension methods from `Mappings/` folders

### Repository Interface Locations
- Legacy: `Common/Interfaces/Persistence/` (IUserRepository, IProductRepository, IRecipeRepository)
- Newer: `{Feature}/Common/` (IMealRepository, IShoppingListRepository, etc.)
- Methods: async with CancellationToken, strongly-typed IDs, return domain entities

## DI Registration (`DependencyInjection.cs`)
`AddApplication()` registers: MediatR + pipeline behaviors, FluentValidation validators, and 5 application services. Repository interfaces are NOT registered here — they go in Infrastructure.

## Constraints

- References only `FoodDiary.Domain`
- No direct infrastructure access (DB, HTTP, file system)
- Never use `DateTime.UtcNow` — use `IDateTimeProvider`
- Never use `CancellationToken.None` — accept tokens from callers
- No `IOptions<>` or `IConfiguration` — configuration belongs in Infrastructure/Web.Api
