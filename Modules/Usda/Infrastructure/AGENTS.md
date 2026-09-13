# USDA Infrastructure

Own the USDA repository and complete `AddUsdaModule` facade. Provider transport/options/detail cache live in Providers and are composed separately by AddUsdaProvider. Preserve legacy CLR/logger names and all cache/cancellation limits. Shared HTTP helpers stay in Integrations; the module owns its five-entity runtime UsdaDbContext. Repositories receive only their owned sets and remain no-tracking/read-only. Initializer retains reference-data import through the central context; migrations and composed reads keep their existing owners.

Shared HTTP bounds, URI validation and integration telemetry are owned by `Shared/FoodDiary.Integrations.Http`. Reference that narrow project directly; provider helpers must not acquire the Billing/mail bridge assembly `FoodDiary.Integrations`.
