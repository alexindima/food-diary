# Notifications consumer contracts

Own notification creation requests, writer, deduplication and client refresh
capabilities, notification type constants and immutable serialized payloads.
Use canonical project and folder namespaces and preserve payload JSON. Depend on
Users.Domain.Contracts for UserId and the shared mediator for cleanup requests. Never expose
notification aggregates, repository ports, delivery adapters or provider SDKs.

The owner writer constructs the aggregate. SaveChanges and transaction ownership
remain with the caller; this extraction does not introduce a queue or new I/O.
