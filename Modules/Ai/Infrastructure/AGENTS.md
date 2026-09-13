# Ai module guidelines

Own repositories, prompt cache and complete module DI. Central Infrastructure references Model only; runtime adapters use AiDbContext. The central context retains migrations and the coordinated purge bridge. Do not alter transaction boundaries, lock order, retry strategy or provider compensation. Exclude Model sources from this project. Keep quota-orphan metrics on the existing shared meter via internal friend access.

Own OpenAI transport/request construction/error metadata/options in Providers. Hosts explicitly call AddAiProvider; preserve legacy CLR/logger names and the unchanged shared Integrations meter. Shared response-bound helpers remain in Integrations, which must not reference Ai back. No live provider calls during verification.

Shared HTTP bounds, URI validation and integration telemetry are owned by `Shared/FoodDiary.Integrations.Http`. Reference that narrow project directly; provider helpers must not acquire the Billing/mail bridge assembly `FoodDiary.Integrations`.

Usage reporting delegates to IAiUsageQuery, implemented by host ReadModel.Composition. AiUsageRepository receives only its owned DbSet for staged writes; hosts must register AddReadModelComposition. Quota and job-store transaction boundaries remain unchanged.

AiDbContext owns five root mappings and prompt revisions. Scoped usage/template writes join the shared connection through CreateModuleContext and synchronize the live transaction before each owner operation. Independent quota/job contexts receive copied provider options and preserve per-operation contexts, execution strategy, locks and local commits; never bind them to the scoped owner context. Keep five-minute prompt caching and fallbacks unchanged.
