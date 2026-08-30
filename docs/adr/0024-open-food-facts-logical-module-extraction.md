# 0024: Extract the OpenFoodFacts logical module

- Status: Accepted
- Date: 2026-08-30

## Context

OpenFoodFacts cache orchestration, ports, model, durable entity, repository, and EF mapping were split across legacy central projects. Products consumed the application implementation assembly, while the provider adapter correctly lived in Integrations. This obscured cache lifecycle ownership and made the provider boundary look like catalog ownership.

## Decision

Move the owned application, abstractions, contracts, domain, infrastructure, persistence-model, and focused-test surfaces to `Modules/OpenFoodFacts`. Preserve the legacy `FoodDiary.Application.OpenFoodFacts` application assembly name and all existing CLR namespaces. Products references only `FoodDiary.Modules.OpenFoodFacts.Contracts`; Integrations implements the module-owned `IOpenFoodFactsService` port. The shared `FoodDiaryDbContext`, historical migrations, and snapshot remain central, with an explicit persistence-model registration seam.

The provider's bounded response reading, request cancellation, 15-second search timeout, concurrency/in-flight limits, single-flight refresh, fresh/stale in-memory fallback, warning logging, and telemetry remain behaviorally unchanged.

## Consequences

- Cache lifecycle ownership is explicit and independent of provider transport.
- EF model identity and database schema remain stable; no migration is introduced.
- Hosts compose the module through Infrastructure's `AddOpenFoodFactsModule` facade.
- Central Infrastructure depends only on module Domain and PersistenceModel, avoiding a dependency cycle.
- OpenFoodFacts provider adapter tests remain with Integrations-focused central tests because the adapter itself remains in `FoodDiary.Integrations`.
