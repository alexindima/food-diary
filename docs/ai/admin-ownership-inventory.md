# Admin extraction ownership inventory

Baseline: `4c1b1c2c31a0886cf7e01d8bba7ec00231e6b149`, clean detached
worktree before branch `codex/admin-module-extraction` was created.

## Physical ownership decision

| Source responsibility | Destination / retained owner and evidence |
| --- | --- |
| 135 production C# files in FoodDiary.Application.Admin: commands, queries, validators, mappings, read orchestration and DI | Modules/Admin/Application; preserve FoodDiary.Application.Admin assembly and CLR namespaces. Other owners' capabilities are consumed unchanged. |
| AdminBilling ports and six read/filter models | Modules/Admin/Application/Abstractions. AdminBillingRepository performs read-only joins over Billing and Users for administrative reports; it grants no Billing write ownership. |
| AdminImpersonationSession repository ports and read model | Modules/Admin/Application/Abstractions. StartAdminImpersonationCommandHandler creates the session; AdminAuditReadService reads it. |
| AdminImpersonationSession entity | Modules/Admin/Domain. It uses scalar UserId only. Its EF configuration has two HasOne<User>().WithMany() relationships, no inverse User navigation. No central Domain consumer was found. The dependency remains one-way toward central Domain. |
| AdminImpersonationSessionConfiguration | Modules/Admin/Infrastructure/Model, explicitly registered by the shared context. Keep CLR namespace, keys, indexes, lengths, timestamp type and Restrict deletion unchanged. |
| AdminBillingRepository and AdminImpersonationSessionRepository | Modules/Admin/Infrastructure. Preserve SQL, pagination, escaping, tracking, cancellation and scoped interface aliases. |
| IAdminImpersonationHandoffService and specialized handoff implementation | Admin ports and Infrastructure respectively. The implementation preserves the imp_ / impersonation: namespaces, cryptographic code generation and two-minute TTL. IAdminSsoCodeStore and its memory/Redis providers, SSO service and JWT generator remain the central Identity runtime boundary. |
| IAdminMailInboxReader, errors and four mail projection models | Admin application ports. MailInboxReader and approved supporting-service client references remain in Integrations. No provider, MIME, mail delivery or retention implementation moves. |
| User, Role, UserRoleAuditEvent, administrative User projections/mutations and AdminUserRoleAuditRepository | Retain Users/central ownership. BACKEND_MODULE_OWNERSHIP assigns role audit to Users; UserAdministrationReadService and UserAdministrationMutationService own user behavior. The existing legacy Admin namespace of audit ports does not transfer ownership. |
| EmailTemplate, EmailTemplateRepository/provider/configuration and template ports/models | Retain Identity/Email ownership. Identity/Email/Services/EmailTemplateAdministrationService directly implements template mutation; Admin calls that capability. No email-owned file moves simply because it lives under an Admin folder/namespace. |
| Shared DbContext, DbSets, migration history/snapshot and UserCleanupService | Remain central. Cleanup deletes sessions for actor/target user as part of the existing personal-data lifecycle. Model registration is explicit; no schema migration is intended. |
| HTTP authorization, consent, SSO/exchange routes, bootstrap, structured audit, jobs and outbox replay | Remain their current presentation, Identity, runtime and host responsibilities. No policy, token issuance, retry, transaction or delivery behavior redesign. |

## Compatibility and composition

Physical project identity is distinct from CLR namespaces and runtime composition.
The application retains its legacy AssemblyName; relocated types retain namespaces.
New assembly locations require coordinated host rebuilds, not an assertion that old
binaries can run unchanged. Central Abstractions may reference Admin ports one-way
for the existing Errors.MailInbox forwarding facade. Admin ports depend only on
Admin Domain and shared Results, so this introduces no reverse dependency.
Central Infrastructure directly references Admin PersistenceModel and consumes its transitive Admin Domain; it never references Admin Infrastructure.
Hosts compose Admin persistence explicitly; JobManager must not acquire additional
application handlers merely to preserve the old persistence registrations.

## Test ownership

Move Admin-only validator/query/handler tests which substitute foreign capabilities
to Modules/Admin/tests. Keep the AdminFeatureTests partial family, AdminLessonFeatureTests,
UserLoginActivityFeatureTests and UserAdministrationMutationServiceTests central:
they exercise concrete Users/Identity/Lessons/Ai/ContentReports implementations.
CreateAdminUserCommandHandlerTests uses Users mapping and remains mixed coverage.
AdminInvariantTests belongs with Admin Domain. Handoff-service tests use the real
central SSO store and compare both protocols, so stay central as mixed coverage. Existing PostgreSQL AdditionalPersistenceRepositoryIntegrationTests
and UserCleanupServiceIntegrationTests use the shared fixture and cover multiple
owners, so remain central. Presentation, HTTP, host, JobManager and mixed Gamification
tests retain their owners and assertions.

Required execution evidence: module tests, real central donor/consumer suites,
full ArchitectureTests, relevant HTTP/Swagger tests, unfiltered PostgreSQL
Infrastructure.IntegrationTests and EF pending-model comparison. This inventory
is source-review evidence, not a statement that those checks have run.

## Wiki discovery observations

The original Admin page finds all major source areas but labels the entity inventory
unpopulated and treats legacy Admin-namespaced Email/User audit contracts as Admin's
public surface. Its SSO and mixed-test listings are discovery leads, not ownership
proof. Verify these with current code and the stronger ownership guide.

## Review of preserved behavior

The normalized source audit compares each moved C# file with the exact base blob:
163 unchanged bodies and two intentional application composition/assembly-access
edits. SQL read projections, wildcard escaping, pagination, cancellation propagation,
transaction ownership and EF field/index/delete definitions are unchanged. Module
DI preserves scoped repository aliases and the singleton handoff lifetime.

AdminImpersonationSession stores scalar actor/target IDs with no User inverse
navigation. Central cleanup still removes those sessions through the shared context.
Authentication, JWT issuance, SSO code storage/Redis, initial-admin bootstrap and
structured audit are untouched. The specialized handoff retains random 32-byte
codes, protocol-specific prefixes, single-use store consumption and a two-minute
TTL. Email administration remains an Identity capability; role audit remains Users.
No broader permission, provider retry, outbox or retention behavior is introduced.

The new assembly placement requires a coordinated rebuild/publish of the hosts.
The application AssemblyName and all moved CLR namespaces are retained; old compiled
Domain/Infrastructure consumers are not claimed binary-compatible. No database
migration, wire-contract change or deployment was performed. Rollback means restoring
the complete prior host build, with no schema rollback needed if EF confirms the
intended unchanged model. Production validation remains an integration/deployment
responsibility and was not attempted from this worktree.
