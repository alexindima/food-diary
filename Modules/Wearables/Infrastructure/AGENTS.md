# Wearables Infrastructure Guidelines

- Own repositories, serialized transaction runner, OAuth-state protection, token protection, and complete `AddWearablesModule` registration.
- Own Fitbit HTTP clients/options in Providers and AddWearablesProvider. Preserve legacy CLR names, OAuth/config validation, response bounds and the aggregate request deadline; common helpers remain in Integrations without a reverse module reference.
- Keep external calls outside advisory-lock/database transactions and never log protected or plaintext tokens.
- Reference the central Infrastructure project only for the shared `DbContext` and proven common persistence primitives.
