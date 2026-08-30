# OpenFoodFacts Infrastructure Guidelines

Own durable cache persistence and the complete `AddOpenFoodFactsModule` composition facade. Reuse central `FoodDiaryDbContext` and `IUnitOfWork`; central Infrastructure references only this module's Domain and PersistenceModel projects. Provider HTTP behavior stays in Integrations.
