# Notifications consumer contracts

Own notification creation requests, writer, deduplication and client refresh
capabilities, notification type constants and immutable serialized payloads.
Preserve FoodDiary.Application.Abstractions.Notifications namespaces and existing
payload JSON. Depend only on Users.Domain.Contracts for UserId. Never expose
notification aggregates, repository ports, delivery adapters or provider SDKs.

The owner writer constructs the aggregate. SaveChanges and transaction ownership
remain with the caller; this extraction does not introduce a queue or new I/O.
