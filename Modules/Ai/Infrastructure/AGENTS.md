# Ai module guidelines

Own repositories, prompt cache and complete module DI. Central Infrastructure references PersistenceModel only; runtime adapters use AiDbContext. The central context retains migrations and the coordinated purge bridge. Do not alter transaction boundaries, lock order, retry strategy or provider compensation. PersistenceModel is a sibling project. Keep quota-orphan metrics on the existing shared meter via internal friend access.

Own OpenAI transport/request construction/error metadata/options in Providers. Hosts explicitly call AddAiProvider; use project-and-folder namespaces and the unchanged shared Integrations meter. Shared response-bound helpers remain in Integrations, which must not reference Ai back. No live provider calls during verification.

Shared HTTP bounds, URI validation and integration telemetry are owned by `Shared/FoodDiary.Integrations.Http`. Reference that narrow project directly; provider helpers must not acquire the Billing/mail bridge assembly `FoodDiary.Integrations`.

Usage readers consume IAiUsageQuery directly, implemented by host ReadModel.Composition. Usage is written only by AiQuotaRepository during reconciliation; do not restore the unused standalone usage writer. Hosts register AddReadModelComposition for reporting. Quota and job-store transaction boundaries remain unchanged.

AiDbContext owns five root mappings and prompt revisions. Scoped template writes join the shared connection through CreateModuleContext and synchronize the live transaction before each owner operation. Independent quota/job contexts receive copied provider options and preserve per-operation contexts, execution strategy, locks and local commits; never bind them to the scoped owner context. Keep five-minute prompt caching and built-in fallback texts unchanged.

Register the concrete scoped AiPromptTemplateRepository once and map its read-model and write ports to that same instance. Declare direct references to AI Domain and Images.Contracts for the entity and image-ID types used here.

Prompt selection uses LanguageCode.FromPreferred: active requested locale, active English, then the built-in fallback. Cache keys include normalized locale; retain the five-minute TTL. Register one scoped FoodRecognitionJobStore and map its reader and writer ports to the same instance.

AiDbContext translates only unique violations for public.AiPromptTemplates / IX_AiPromptTemplates_Key_Locale into DbUpdateConcurrencyException. Keep other database errors unchanged. Failed inserts are rolled back by the caller; never retry the failed tracked scope.
