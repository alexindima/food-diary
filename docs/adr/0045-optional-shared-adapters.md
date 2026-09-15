# ADR 0045: Explicit shared adapter composition

Status: Accepted

## Context

Registering persistence coordination also registered audit readers/writers, email
delivery and outbox replay management. This made an execution mechanism select
optional services and made its assembly own their implementations.

## Decision

Keep FoodDiary.Persistence.Runtime responsible for the shared EF model, session,
save coordination, transactions and database telemetry. Keep the existing shared
record mappings and migration owner unchanged.

Move audit adapters to Shared/FoodDiary.Audit.Infrastructure and email adapters
to Shared/FoodDiary.Email.Infrastructure. Hosts explicitly call
AddAuditInfrastructure and AddEmailInfrastructure after persistence registration.
Both adapters consume the same scoped SharedPersistenceDbContext. They must not
create an independent context, save or commit while enqueueing records.

Register replay management explicitly through AddOutboxReplayManagement. Its
implementation remains in Persistence.Runtime because it uses the internal shared
transaction reset mechanism. Do not expose that mechanism just to move a class.
Email owns its replay stream; the coordinator remains independent of stream names.

Identity.Contracts owns the 11 Google/Telegram/SSO protocol error factories used
by Identity implementations. Preserve their existing error codes, messages and
kinds. Shared authentication/account errors also consumed by Users remain shared;
do not introduce a reverse Users-to-Identity dependency.

Keep Authentication.Infrastructure: JWT configuration is shared by issuer and
validator, and SSO storage is shared by Identity and Admin. Keep Application.Runtime
as the host-composed execution pipeline; modules depend on its contracts.

## Consequences

The persistence coordinator no longer references audit/email service contracts
or registers those services implicitly. Two narrow adapter assemblies are added;
project count is secondary to ownership and dependency direction. Audit record
internals are accessible to the audit adapter without becoming a public API.

There are no database, endpoint, payload or configuration-key changes. The
structured audit logger's category follows its new adapter namespace; fields,
level and clock remain unchanged. Hosts and tests needing all services must
compose all registrations explicitly. Runtime-only consumers can omit them.
