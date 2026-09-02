# Domain Primitives Guidelines

## Scope
Rules for `Shared/FoodDiary.Domain.Primitives/`.

## Role
- Own domain model primitives shared by backend bounded contexts.
- Keep this package generic and independent from FoodDiary application, infrastructure, presentation, host, and feature projects.

## Rules
- Do not add feature-specific entities, value objects, domain events, repositories, handlers, or services here.
- Do not reference ASP.NET, EF Core, provider SDKs, or application/infrastructure projects.
- Keep public abstractions small and stable; changes here can affect every backend domain module.
- Keep domain entities auditable through `Entity<TId>` unless a future bounded context explicitly proves it needs a separate primitive.
- Require `DateTimeKind.Utc` for audit timestamps and domain-event occurrence timestamps; convert external values before they enter these primitives.
- Keep entity identifiers immutable after initialization so equality and hash codes remain stable.
- Use `DomainTime.Override(TimeProvider)` only as a disposed scope for deterministic domain execution; do not introduce a process-global mutable clock.

## Commands
- Build: `dotnet build Shared/FoodDiary.Domain.Primitives/FoodDiary.Domain.Primitives.csproj`

## Generic input validation

Public DomainGuard uses this owner's FoodDiary.Domain.Primitives namespace. Keep its generic numeric, enum, text, JSON syntax and UTC-normalization contracts stable. RequiredUtc deliberately accepts Local and converts with ToUniversalTime; this input normalization differs from strict UTC audit invariants above. JSON parsing validates syntax only. Billing storage precision and currency policies belong to BillingDomainGuard, never this library.
