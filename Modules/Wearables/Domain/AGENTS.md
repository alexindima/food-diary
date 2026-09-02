# Wearables Domain Guidelines

- Own `WearableConnection`, `WearableSyncEntry`, provider/data enums, identifiers, and `ProtectedWearableToken` with preserved CLR identity.
- Central `User`/`UserId` remain compatibility dependencies; do not move them into this module.
- Keep token plaintext out of domain state. Preserve provider/user uniqueness and UTC/date invariants.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and residual central Domain only for shared values. Preserve all existing relationships.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access. This domain has no central Domain dependency.
