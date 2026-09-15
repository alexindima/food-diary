# Shared persistence runtime

Own the module-independent runtime context, scoped persistence session, coordinated
saves, transactions, audit storage, email outbox, replay coordination and database
telemetry. Use canonical `FoodDiary.Persistence.Runtime` namespaces.

- Never reference the complete FoodDiary.Infrastructure model, read composition,
  module Domain, PersistenceModel, Infrastructure, Application or host assemblies.
  Users.Domain.Contracts supplies the existing scalar UserId used by shared audit.
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
