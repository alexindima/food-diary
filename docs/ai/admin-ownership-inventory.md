# Admin ownership inventory

This document describes the current module layout. Historical extraction steps and
verification runs belong in Git history; they are not current ownership rules.

## Projects and namespaces

The seven production projects are siblings under `Modules/Admin`: Application,
Application.Abstractions, Contracts, Domain, Infrastructure, PersistenceModel and
Presentation. Each namespace follows the project filename and physical folders;
there are no RootNamespace overrides. Application retains the assembly name
`FoodDiary.Application.Admin`; its namespaces start with
`FoodDiary.Modules.Admin.Application`.

Presentation has Controllers, Requests, Responses, Mappings and Extensions folders.
It owns HTTP transport and calls Application through request mappings. Application
references its own Domain explicitly; Presentation has no direct Domain reference.

## Ownership

| Concern | Owner and boundary |
| --- | --- |
| Administrative commands and queries | Admin/Application. Foreign user, email, lesson and other operations use their owning module's contracts. |
| ExchangeAdminImpersonationCommand | Admin/Contracts; consumed by Identity Presentation. Handlers remain in Admin/Application. |
| Administrative read ports and projection models | Admin/Application.Abstractions. A port's location does not transfer ownership of foreign aggregates. |
| AdminImpersonationSession | Admin/Domain. Actor and target are scalar UserId values from Users/Domain.Contracts; there are no foreign aggregate navigations. |
| Session writes | Admin/Infrastructure/Persistence/AdminImpersonationSessionRepository implements IAdminImpersonationSessionWriteRepository and receives only the owned DbSet. It neither reads projections nor saves independently. |
| Session reads | IAdminImpersonationSessionQuery is implemented by FoodDiary.ReadModel.Composition/Admin/AdminImpersonationSessionQuery. GetAdminImpersonationSessionsQueryHandler consumes it directly. |
| Billing and role-audit SQL projections | FoodDiary.ReadModel.Composition/Admin, including their DI registrations. Billing and Users retain aggregate ownership. |
| Session and receipt EF model | Admin/PersistenceModel. AdminDbContext tracks the two owned entities; the central model also applies their configuration. Foreign User relationships are composed in FoodDiary.Infrastructure/Persistence/Composition/AdminCrossModuleRelationships.cs. |
| BugAcknowledgementReceipt | Admin/PersistenceModel. Stores the inbox ID for deduplication. Admin Infrastructure handles receipt persistence; SendBugAcknowledgementsCommandHandler owns the application workflow; the worker dispatches its Contracts request through ISender. |
| Shared persistence and migrations | FoodDiary.Infrastructure. Shared save/transaction coordination and user purge remain in the existing central lifecycle. |
| Impersonation handoff | Admin Infrastructure owns the specialized adapter. Authentication token issuance and SSO storage remain behind their existing owner contracts. |
| MailInbox bridge and acknowledgement worker | Admin Infrastructure. Supporting-service access uses the approved MailInbox client. The application depends on ports, not that client. |
| Dashboard summary | AdminDashboardReadService is reused by the summary and overview handlers. |

## Composition

AddAdminModule registers Application and Admin persistence. AddAdminPersistence
registers the owned runtime context, the write-only session repository, purge
participant and handoff service. Resolving the writer does not require a read
projection or ReadModel.Composition. Hosts register AddReadModelComposition
separately when they need administrative projections. MailInbox and background
acknowledgement integrations retain explicit host registration.

There is no combined session repository or separate legacy read-repository alias.
Query and write contracts represent separate dependencies. Shared contexts still
coordinate persistence; this split does not change transaction semantics or schema.

## Tests and guardrails

Admin's five test projects live under Modules/Admin/tests. Application includes the
AdminFeatureTests family and administrative user/lesson scenarios, even where they
exercise another owner's public capability. Domain owns session invariants;
Infrastructure owns handoff, MailInbox, worker and registration tests; Presentation
owns Admin controller and mapping tests. Infrastructure.IntegrationTests owns the
focused PostgreSQL role-audit projection cases.

Central suites retain cross-module concerns, including
SharedAdminContextCompositionIntegrationTests and
AdditionalPersistenceRepositoryIntegrationTests, shared HTTP conventions and host
composition. Linked PostgreSQL/support fixtures retain their original owners.

AdminNamespaceTests and PhysicalProjectLayoutTests protect project/folder naming.
ProjectDependencyMatrixTests protects direct project references.
ControllerConventionsTests covers Admin controller discovery and rejects direct
Application references for both legacy and module namespaces. Registration tests
prove the session writer resolves without read composition and remains scoped.

Use scoped test commands from AGENTS.md. Test execution results are reported per
change; this inventory does not claim that a particular suite has been run.

See [ADR 0038](../adr/0038-read-model-composition.md) for composed reads and the scoped
AGENTS.md files for implementation rules.
