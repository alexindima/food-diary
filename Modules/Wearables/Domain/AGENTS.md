# Wearables Domain Guidelines

- Own `WearableConnection`, `WearableSyncEntry`, provider/data enums, identifiers, and `ProtectedWearableToken` with preserved CLR identity.
- Central `User`/`UserId` remain compatibility dependencies; do not move them into this module.
- Keep token plaintext out of domain state. Preserve provider/user uniqueness and UTC/date invariants.
