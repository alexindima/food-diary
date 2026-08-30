# Wearables Infrastructure Guidelines

- Own repositories, serialized transaction runner, OAuth-state protection, token protection, and complete `AddWearablesModule` registration.
- Keep provider HTTP clients/options in `FoodDiary.Integrations`.
- Keep external calls outside advisory-lock/database transactions and never log protected or plaintext tokens.
- Reference the central Infrastructure project only for the shared `DbContext` and proven common persistence primitives.
