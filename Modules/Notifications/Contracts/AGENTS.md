# Notifications consumer contracts

Own notification creation requests, writer, deduplication and client refresh
capabilities, notification type constants and immutable serialized payloads.
Use canonical project and folder namespaces and preserve payload JSON. Depend on
Users.Domain.Contracts for UserId and the shared mediator for cleanup requests. Never expose
notification aggregates, repository ports, delivery adapters or provider SDKs.

The owner writer constructs the aggregate. SaveChanges and transaction ownership
remain with the caller; this extraction does not introduce a queue or new I/O.

NotificationIntent factories bind the appropriate payload, type and target; immutable NotificationRequest retains that intent. FromLegacy is the explicit compatibility seam preserving unknown type/JSON/reference values. RecommendationCommentTarget owns recipient/composite targets and the clientId|recommendationId codec; retain Guid formatting, malformed-reference fallback and payload JSON field names.
