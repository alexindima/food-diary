# Backend Time Policy

Date: 2026-03-28

## Goal

Keep backend time-dependent behavior deterministic, testable, and consistent across layers.

## Rules By Layer

### Application

- Use .NET `TimeProvider` for current UTC time.
- Do not call `DateTime.UtcNow` directly in handlers, services, or validators.

### Presentation

- Do not call `DateTime.UtcNow` directly in controllers or HTTP mappings.
- Inject `TimeProvider` when presentation code needs current UTC time.
- Keep controllers and HTTP mappings deterministic in tests by using a fixed `TimeProvider`.

### Infrastructure

- Use `TimeProvider` for operational timestamps, expirations, quota windows, and repository-side defaults.
- Direct `DateTime.UtcNow` is allowed only in generated EF migration snapshots or other generated artifacts.

### Domain

- Domain time policy is intentionally stricter and needs explicit design decisions.
- Until the domain refactor pass, avoid broad churn.
- New domain code should prefer receiving normalized timestamps from callers when practical.
- Existing direct clock usage in domain is tracked separately and should be reduced deliberately, not opportunistically.

## Approved Exceptions

- Generated files may contain fixed or generated UTC timestamps.
- Tests may use `DateTime.UtcNow` when they are explicitly testing relative current-time behavior, but fixed `TimeProvider` implementations are preferred for deterministic behavior.

## Immediate Migration Order

1. Presentation mappings and controllers
2. Infrastructure services and repositories
3. Application leftovers
4. Domain audit fields and events in a dedicated pass
