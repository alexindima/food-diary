# Ai module guidelines

Own repositories, prompt cache and complete module DI. Central Infrastructure references Model only; adapters reference shared DbContext. Do not alter transaction boundaries, lock order, retry strategy or provider compensation. Exclude Model sources from this project. Keep quota-orphan metrics on the existing shared meter via internal friend access.

Own OpenAI transport/request construction/error metadata/options in Providers. Hosts explicitly call AddAiProvider; preserve legacy CLR/logger names and the unchanged shared Integrations meter. Shared response-bound helpers remain in Integrations, which must not reference Ai back. No live provider calls during verification.

Shared HTTP bounds, URI validation and integration telemetry are owned by `Shared/FoodDiary.Integrations.Http`. Reference that narrow project directly; provider helpers must not acquire the Billing/mail bridge assembly `FoodDiary.Integrations`.
