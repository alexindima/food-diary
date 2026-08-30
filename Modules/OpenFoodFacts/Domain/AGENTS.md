# OpenFoodFacts Domain Guidelines

Own the durable external-catalog cache entity and its invariants. Preserve `FoodDiary.Domain.Entities.OpenFoodFacts.OpenFoodFactsProduct` identity. Reference only central Domain while `DomainGuard` remains shared; do not reference application, persistence, provider, or host concerns.
