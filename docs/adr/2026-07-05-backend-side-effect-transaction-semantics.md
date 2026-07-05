# Backend Side Effect And Transaction Semantics

## Status
Accepted

## Context
The primary FoodDiary backend is a modular monolith with application commands committed through the command transaction pipeline. Domain events are published from the EF Core `SavingChanges` interceptor, so handlers run before the database commit and may add transactional state.

This makes side-effect placement important. Sending email, push notifications, storage deletes, provider calls, and similar operations must not happen from domain event handlers or normal command mutation paths before the transaction commits.

## Decision
- Application commands persist state through `IUnitOfWork` and the command transaction pipeline.
- Domain event handlers may write transactional state, including outbox records, but must not directly call external transports or providers.
- Business email dispatch is outbox-only from application services. Direct email transport is reserved for explicit diagnostic/test commands.
- Non-transactional post-commit UI notifications may use `IPostCommitActionQueue`; durable external work should prefer a database outbox processed by `FoodDiary.JobManager`.
- Explicit transaction runners remain narrow exceptions for workflows that need their own transactional boundary, such as billing webhook/renewal updates.

## Consequences
- If a process stops after commit but before a side effect, durable outbox work can still be retried.
- Diagnostic test sends may still fail synchronously because their purpose is to verify the active transport path.
- New side-effect paths should add or reuse an outbox instead of injecting provider transports into domain event handlers.
- Architecture tests should continue guarding domain event handlers from direct side-effect dependencies.
