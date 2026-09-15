# Dietologist Logical Module Guidelines

## Scope

Rules for `Modules/Dietologist/`.

## Boundaries

- Own Dietologist commands, queries, services, policies, module ports, aggregates, persistence adapters, and EF configurations.
- Preserve legacy `FoodDiary.Application.Dietologist.*`, `FoodDiary.Application.Abstractions.Dietologist.*`, `FoodDiary.Domain.*`, and persistence CLR namespaces and the application assembly identity.
- Keep client health data behind relationship authorization and explicit `DietologistPermissions`; no consumer may bypass `IDietologistDashboardAccessService` or the attention-signal projection.
- Register application behavior through `AddDietologistApplication`; composition roots use Infrastructure's `AddDietologistModule` facade.
- Keep the shared `FoodDiaryDbContext`, historical migrations, and model snapshot in central Infrastructure.
- Own collaboration audit rule selection in Infrastructure's `CollaborationAuditInterceptor`.
  `AddDietologistModule` registers one scoped EF `ISaveChangesInterceptor` with
  `TryAddEnumerable`; central persistence installs module interceptors after domain
  event dispatch. Preserve synchronous/asynchronous SaveChanges timing and the
  shared transaction. AuditEntry/model belong to Shared/FoodDiary.Audit.PersistenceModel; the writer and coordinated save stay central. The shared audit model IVT
  grants internal storage access without making a public audit entity API.
- Focused audit rule/composition tests live in the existing module Infrastructure
  tests. Central PostgreSQL tests retain the shared dispatch/transaction boundary.
- Keep the Dashboard access service and permission projection in Contracts; Dashboard references it directly. Internal repositories and adapter-facing ports stay in Application/Abstractions.

## Verification

- Application tests: `dotnet test Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/FoodDiary.Modules.Dietologist.Application.Tests.csproj`
- Domain tests: `dotnet test Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Domain.Tests/FoodDiary.Modules.Dietologist.Domain.Tests.csproj`
- Infrastructure tests: `dotnet test Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Infrastructure.Tests/FoodDiary.Modules.Dietologist.Infrastructure.Tests.csproj`
- Architecture: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

## Error ownership

Feature error factories belong to their existing owner contracts; call them directly.
The corresponding central Errors facades are retired. Preserve exact codes, messages,
kinds and parameter formatting. Reference the owner explicitly; this grants no foreign
repository or aggregate capability. See docs/ai/feature-error-retirement.md.

Dietologist PersistenceModel consumes Users.Domain.Contracts for scalar IDs. Central DietologistCrossModuleRelationships owns its ten User FKs: seven Cascade, two ClientTask Restrict, one optional invitation DietologistUserId SetNull. Preserve IsRequired(false), local Recommendation relationships, xmin and indexes. Do not move permissions, access checks or audit behavior into composition.

AttentionSignalMetricsReadService is implemented and registered by ReadModel.Composition. Dietologist retains its port, relationship authorization, permission filtering and signal calculation. The composition adapter preserves batch SQL projections over Users, Meals and BodyMetrics; it never returns aggregates or tracks writes. Audit interception and runtime persistence remain in Dietologist.

Joined invitation, recommendation and comment DTO reads are implemented and registered by ReadModel.Composition through the existing read-model repository ports. Combined repository methods delegate for compatibility; write/read aggregate aliases remain owner-scoped. Do not alias read-model ports back to the combined repositories: this creates a dependency cycle. Keep authorization in Application and persistence/audit in the owner.

DietologistDbContext owns the six collaboration entities at runtime. Repositories
receive owner sets (the invitation repository uses the typed context for detached
reconciliation). Central mappings and ten User FKs remain migration-owned.
CollaborationAuditInterceptor inspects the save tracker plus registered
Dietologist entries through IModuleChangeTrackerSource during the central save, before owner persistence. Pending
audit entries are reused across retries; successful saves/reset release their
tracking state. Coordinated saves always invoke central interception, including
when only an owner changed. Audit and owner writes require a relational provider;
non-relational coordinated audit is rejected before persistence. User purge uses
DietologistDbContext and rebinds the live coordinator transaction on each call.
Preserve order 40 and both client/dietologist predicates. The audit interceptor retains central AuditEntry storage access, but does not depend on the concrete shared context. See ADR 0040 and shared PostgreSQL tests.

IModuleChangeTrackerSource is implemented by the save context, not resolved through DI; this avoids creating a context/interceptor dependency cycle. Inspect only DietologistDbContext entries, preserve registration order and DetectChanges behavior, and keep the existing synchronous/asynchronous central save timing and pending-event deduplication.

Dietologist Infrastructure references Audit.PersistenceModel directly and has no central Infrastructure project reference or friend grant. Its infrastructure tests explicitly reference central Infrastructure and persistence abstractions for coordinated-save fixtures. The shared audit model retains its exact existing Dietologist friend grant.
