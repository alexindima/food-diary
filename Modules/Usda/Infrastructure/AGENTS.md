# USDA Infrastructure

Own the USDA repository and complete `AddUsdaModule` facade. Provider transport/options/detail cache live in Providers and are composed separately by AddUsdaProvider. Preserve legacy CLR/logger names and all cache/cancellation limits. Shared HTTP helpers stay in Integrations; central Infrastructure owns DbContext and migrations.
