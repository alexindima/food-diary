# OpenFoodFacts Infrastructure Guidelines

Own durable cache persistence and the complete `AddOpenFoodFactsModule` composition facade. Own `OpenFoodFactsDbContext`; reuse the shared connection and a live transaction accessor supplied by registration. Synchronize the current shared transaction before reads and immediate SQL upserts; do not begin or complete caller transactions. Keep `IUnitOfWork` behavior unchanged; central Infrastructure references only this module's Domain and PersistenceModel projects. Provider HTTP/options/cache behavior lives in Providers with explicit AddOpenFoodFactsProvider registration. Preserve legacy CLR/logger names, retry/timeout/cache limits and logging suppression; shared helpers remain in Integrations with a one-way reference.

Shared HTTP bounds, URI validation and integration telemetry are owned by `Shared/FoodDiary.Integrations.Http`. Reference that narrow project directly; provider helpers must not acquire the Billing/mail bridge assembly `FoodDiary.Integrations`.

Owner creation and live transaction lookup use IModuleContextFactory and
IModuleTransactionCoordinator. Do not restore a direct or transitive central
Infrastructure project dependency. Read CurrentTransaction inside the callback,
including null after completion; never capture it at repository resolution.
