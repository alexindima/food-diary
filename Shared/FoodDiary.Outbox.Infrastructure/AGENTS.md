# Shared Outbox Infrastructure

- Own the generic EF/PostgreSQL claiming and processing lifecycle, retry policy, processing options and outbox telemetry for the four existing streams.
- Depend only on narrow outbox lifecycle and persistence contracts; never reference central Infrastructure or business modules.
- Preserve SQL table allowlist, lease/revision fencing, independent finalization timeout, caller cancellation, and safe error logging. External dispatch is never part of provider transaction retry.
- Before clearing tracking or claiming, check IModuleScopeGuard when the supplied context implements it; otherwise reject local pending changes and an active transaction. Owner processors retain their live scoped guard callback before every claim.
- Keep the existing FoodDiary.Infrastructure meter name, instrument names, tags, units and OutboxProcessing configuration key. The meter here owns only outbox instruments.
- Shared replay coordination remains in Persistence.Runtime; email adapters are in Email.Infrastructure. Do not move stream-specific records or provider adapters here.
- Existing central outbox unit/provider tests retain coverage across adapters; generic internal helpers are visible only to those test assemblies.

The technical replay-stream extension contract and tracked-record metadata live here as well; the central coordinator retains all replay transaction, audit and save ownership.

AddOutboxProcessing owns OutboxProcessing binding and startup validation. Hosts
compose it explicitly. Persistence registration must not opt consumers into this
validation. Preserve defaults, the configuration key and validation message.
