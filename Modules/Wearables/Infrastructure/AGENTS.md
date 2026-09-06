# Wearables Infrastructure Guidelines

- Own repositories, serialized transaction runner, OAuth-state protection, token protection, and complete `AddWearablesModule` registration.
- Own Fitbit HTTP clients/options in Providers and AddWearablesProvider. Preserve legacy CLR names, OAuth/config validation, response bounds and the aggregate request deadline; common helpers remain in Integrations without a reverse module reference.
- Keep external calls outside advisory-lock/database transactions and never log protected or plaintext tokens.
- Reference the central Infrastructure project only for the shared `DbContext` and proven common persistence primitives.

Shared HTTP bounds, URI validation and integration telemetry are owned by `Shared/FoodDiary.Integrations.Http`. Reference that narrow project directly; provider helpers must not acquire the Billing/mail bridge assembly `FoodDiary.Integrations`.

The serialized runner holds a separate session advisory lock and executes the provider callback once. EF SaveChanges owns the final atomic transaction and its retries. Never wrap OAuth/HTTP calls in an execution-strategy retry. Failure Results discard pending tracking; intentional token-refresh/deactivation saves inside the callback remain durable. PostgreSQL tests enable production retry settings.
