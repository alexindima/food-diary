# OpenFoodFacts Infrastructure Guidelines

Own durable cache persistence and the complete `AddOpenFoodFactsModule` composition facade. Reuse central `FoodDiaryDbContext` and `IUnitOfWork`; central Infrastructure references only this module's Domain and PersistenceModel projects. Provider HTTP/options/cache behavior lives in Providers with explicit AddOpenFoodFactsProvider registration. Preserve legacy CLR/logger names, retry/timeout/cache limits and logging suppression; shared helpers remain in Integrations with a one-way reference.

Shared HTTP bounds, URI validation and integration telemetry are owned by `Shared/FoodDiary.Integrations.Http`. Reference that narrow project directly; provider helpers must not acquire the Billing/mail bridge assembly `FoodDiary.Integrations`.
