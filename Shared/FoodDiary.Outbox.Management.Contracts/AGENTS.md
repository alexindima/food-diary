# Shared outbox management contracts

Own the dependency-free operator-facing dead-letter listing, audit and replay
contract used by the central coordinator and module stream adapters. Lifecycle
record shape remains in FoodDiary.Outbox.Abstractions; persistence, eligibility,
stream SQL and administrative hosting remain with their existing owners.
