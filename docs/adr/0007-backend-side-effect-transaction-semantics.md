# ADR 0007: Backend Side-Effect And Transaction Semantics

- Status: Accepted
- Date: 2026-07-05
- Owners: Backend
- Related: ADR-0001, ADR-0006
- Supersedes: None

## Context

The primary FoodDiary backend is a modular monolith with application commands committed through the command transaction pipeline. Domain events are published from the EF Core `SavingChanges` interceptor, so handlers run before the database commit and may add transactional state.

This makes side-effect placement important. Sending email, push notifications, storage deletes, provider calls, and similar operations must not happen from domain event handlers or normal command mutation paths before the transaction commits.

## Decision Drivers

- Transactional state changes and external side effects must not report contradictory outcomes.
- Durable work must survive a process stop after the database commit.
- Domain events must remain useful for state changes within the current transaction.

## Considered Options

1. Execute external side effects directly from domain event handlers. This is simple but can publish effects for transactions that later fail and cannot reliably recover after commit.
2. Execute every side effect from an in-memory post-commit callback. This avoids pre-commit effects but loses work when the process stops.
3. Use transactional outboxes for durable external work and reserve post-commit callbacks for non-durable UI refreshes.

## Decision

- Application commands persist state through `IUnitOfWork` and the command transaction pipeline.
- Domain event handlers may write transactional state, including outbox records, but must not directly call external transports or providers.
- Business email dispatch is outbox-only from application services. Direct email transport is reserved for explicit diagnostic/test commands.
- Non-transactional post-commit UI notifications may use `IPostCommitActionQueue`; durable external work should prefer a database outbox processed by `FoodDiary.JobManager`.
- Explicit transaction runners remain narrow exceptions for workflows that need their own transactional boundary, such as billing webhook and renewal updates.

## Consequences

### Positive

- Durable outbox work remains retryable after a process stop following commit.
- External effects cannot escape from domain event handlers before their state is committed.
- Diagnostic transport checks can still expose synchronous provider failures intentionally.

### Negative

- Durable side effects require outbox storage, idempotent processing, and operational monitoring.
- Developers must distinguish transactional events, durable external work, and non-durable post-commit UI work.

## Enforcement

- `tests/FoodDiary.ArchitectureTests/EventGovernanceTests.cs`
- `tests/FoodDiary.ArchitectureTests/SideEffectReliabilityGuardrailTests.cs`
- `tests/FoodDiary.ArchitectureTests/PersistenceTransactionGuardrailTests.cs`

## Follow-up

- New durable side-effect paths must add or reuse an outbox rather than inject provider transports into domain event handlers.
