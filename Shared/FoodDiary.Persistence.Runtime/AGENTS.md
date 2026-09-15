# Shared persistence runtime

Own the module-independent runtime context, scoped persistence session, coordinated
saves, transactions, shared record mappings, replay coordination and database
telemetry. Use canonical `FoodDiary.Persistence.Runtime` namespaces.

- Never reference the complete FoodDiary.Infrastructure model, read composition,
  module Domain, PersistenceModel, Infrastructure, Application or host assemblies.
  Shared record models may retain scalar-contract dependencies.
- AddPersistenceRuntime configures its own provider options; it must work without
  registering FoodDiaryDbContext or its options. Preserve owner interceptors.
- Preserve ADR 0042 connection ownership, retries, save order, late enlistment,
  intermediate-save visibility, cleanup, cancellation and post-commit behavior.
- DI owns context disposal. Module adapters consume Persistence.Abstractions.
- Keep migration classes and the complete model in FoodDiary.Infrastructure.
- Standalone registration tests live under Shared/tests/FoodDiary.Persistence.Runtime.Tests
  and must not reference the full model. Central infrastructure suites retain
  full-model and owner composition integration coverage.

See docs/adr/0043-persistence-runtime-assembly-and-read-facade.md.

ADR 0045 moves audit/email adapters to their narrow Infrastructure assemblies.
AddPersistenceRuntime does not register them. Hosts explicitly compose
AddAuditInfrastructure, AddEmailInfrastructure and AddOutboxReplayManagement.
Replay remains here to use internal transaction reset mechanics without exposing them.

OutboxProcessing option binding/validation belongs to Outbox.Infrastructure and
must be composed explicitly through AddOutboxProcessing(configuration).
