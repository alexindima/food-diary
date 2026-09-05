# USDA Infrastructure

Own the USDA repository and complete `AddUsdaModule` facade. Provider transport/options/detail cache live in Providers and are composed separately by AddUsdaProvider. Preserve legacy CLR/logger names and all cache/cancellation limits. Shared HTTP helpers stay in Integrations; central Infrastructure owns DbContext and migrations.

Shared HTTP bounds, URI validation and integration telemetry are owned by `Shared/FoodDiary.Integrations.Http`. Reference that narrow project directly; provider helpers must not acquire the Billing/mail bridge assembly `FoodDiary.Integrations`.
