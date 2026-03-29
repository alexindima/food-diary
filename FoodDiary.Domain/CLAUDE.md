# FoodDiary.Domain

Pure domain layer with zero external dependencies. Implements DDD tactical patterns.

## Architecture

- **Entity<TId>** (`Common/Entity.cs`) — base class with identity-based equality, audit timestamps (`CreatedOnUtc`, `ModifiedOnUtc`), cached hash code
- **AggregateRoot<TId>** (`Common/AggregateRoot.cs`) — extends Entity, adds domain event raising/clearing via `IAggregateWithEvents`
- **Strongly-typed IDs** (`ValueObjects/Ids/`) — 21 `readonly record struct` wrapping `Guid`, implementing `IEntityId<Guid>`. Pattern: `New()` factory, `Empty` sentinel, implicit-to-Guid / explicit-from-Guid conversions
- **Domain Events** (`Events/`) — sealed records implementing `IDomainEvent` with `OccurredOnUtc`

## Key Patterns

### Factory Method Construction
Every entity uses `private` parameterless constructor (for EF Core) + `public static Create(...)` factory. Child entities use `internal static Create(...)` so only the parent aggregate can instantiate them.

### State Snapshot Pattern
Complex aggregates decompose mutable state into `readonly record struct` state objects (e.g., `MealDetailsState`, `UserCredentialState`). Mutations apply via `with` expressions on the record.

### Update Record Pattern
External callers pass partial updates via `*Update` records with nullable fields and `Clear*` booleans. `EnsureClearConflict` validates no simultaneous clear + set.

### Strict Invariant Enforcement
All mutations validate immediately via `ArgumentException` throws. No Result/Option types in the domain. Common helpers: `NormalizeOptionalText`, `NormalizeRequiredName`, `RequirePositive`, `RequireNonNegative`, `EnsureUserId`, `NormalizeDate`, `NormalizeUtc`.

### Change Detection / Idempotency
Every mutating method compares new vs. current value; if unchanged, returns without calling `SetModified()`. Double comparisons use epsilon `0.000001`.

### Collection Encapsulation
`private readonly List<T> _items = []` backed by `public IReadOnlyCollection<T> Items => _items.AsReadOnly()`. Mutation only through aggregate methods.

### User Aggregate
Split across 5 partial files by concern: core (`User.cs`), `User.Lifecycle.cs`, `User.Profile.cs`, `User.Credentials.cs`, `User.Admin.cs`.

## Naming Conventions

- Entities: `sealed class`, PascalCase, organized by subdomain folder under `Entities/`
- IDs: `{EntityName}Id` in `ValueObjects/Ids/`
- State snapshots: `{Context}State`
- Update DTOs: `{Context}Update`
- Events: `{Verb}DomainEvent` suffix
- Private fields: `_camelCase`
- Validation helpers: `Normalize*`, `Ensure*`, `Require*` prefixes

## Constraints

- No external NuGet packages — only BCL
- No domain services — all behavior lives inside entities
- No Result types — invariants enforced via exceptions
- `DomainTime.UtcNow` for all timestamps (never `DateTime.UtcNow` directly)
- `DomainConstants` for shared max-length constants
