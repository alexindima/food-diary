# Wearables Domain Guidelines

- Own `WearableConnection`, `WearableSyncEntry`, provider/data enums, identifiers, and `ProtectedWearableToken` with preserved CLR identity.
- Central `User`/`UserId` remain compatibility dependencies; do not move them into this module.
- Keep token plaintext out of domain state. Preserve provider/user uniqueness and UTC/date invariants.

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access. This domain has no central Domain dependency.
