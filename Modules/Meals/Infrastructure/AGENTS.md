# Meals Infrastructure

Own MealRepository, persistence registration and complete Meals DI. Depend on
central Infrastructure for the shared context. Central Infrastructure must not
reference this adapter project. Preserve query ordering, access predicates,
tracking behavior and cancellation.
