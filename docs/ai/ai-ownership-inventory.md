# AI ownership inventory

AI owns food analysis, prompt administration, usage reporting contracts, quotas and asynchronous recognition. All seven production projects are sibling directories under `Modules/Ai`: Application, Application.Abstractions, Contracts, Domain, Infrastructure, PersistenceModel and Presentation. Namespaces follow the project filename and relative folders. The Application assembly name remains `FoodDiary.Application.Ai` for existing assembly discovery.

## Boundaries

- Contracts exposes administration reads, prompt administration and completed recognition reads with their DTOs. Admin and Meals consume these public capabilities; they do not acquire quota, provider or job-store ports.
- Application.Abstractions owns provider, quota, job-store and repository ports. IAiUsageQuery is implemented by FoodDiary.ReadModel.Composition. AiQuotaRepository writes usage during quota reconciliation; there is no standalone usage writer.
- Application owns command/query handlers, provider/quota orchestration, completed-result validation and background recognition processing. GetUserAiUsageSummary computes its result in the handler. Profile consumers use the existing Users.Contracts IUserAiProfileReadService and UserAiProfileModel directly; no duplicate AI profile adapter or model is needed.
- Domain owns AI usage and prompt entities and IDs. UserId comes from Users.Domain.Contracts; no foreign aggregate navigation is permitted.
- PersistenceModel owns EF mappings and internal quota/job records. Four foreign User/ImageAsset relationships are composed centrally by AiCrossModuleRelationships. Preserve indexes, conversions and delete behavior.
- Infrastructure owns AiDbContext, staged usage and prompt writes, independent quota/job stores, prompt caching, and OpenAI transport under Providers. Shared HTTP bounds and telemetry helpers belong to Shared/FoodDiary.Integrations.Http.
- Presentation owns Controllers, Requests, Responses, Models, Mappings, Hubs, Services and Extensions. FoodRecognitionNotifier publishes invalidation hints; owner-scoped HTTP remains authoritative. Presentation has no direct Domain reference.

## Service decisions

- OpenAiFoodService owns deadlines, consent, prompt/provider calls and quota reconciliation across three operations; retain this workflow.
- ProcessNextFoodRecognitionCommandHandler owns the background claim/vision/nutrition/completion lifecycle. The worker sends its Contracts request through ISender; preserve independent commits and protection against redispatch after uncertain provider outcomes.
- FoodRecognitionResultReader validates ownership, completion and usable results for Meals; retain the public boundary.
- AiPromptAdministrationService owns prompt mutation for Admin; retain the public boundary.
- Administration reads are owner Contracts queries dispatched through ISender; their Application handlers use internal projection ports. Prompt mutation and completed-recognition checks likewise remain in dedicated AI handlers.
- ApplicationAiTelemetry owns shared application instrumentation, not a handler-forwarding service.
- UserAiUsageSummaryReadService and AiUserContextService are retired. Do not reintroduce services solely to forward one handler or copy an identical owner DTO.

## Runtime invariants and verification ownership

Scoped template writes join the central connection and synchronize its live transaction before owner operations. Quotas and recognition jobs use independent short transactions with copied provider options. Preserve lock order, retry strategy and commit boundaries.

Provider I/O follows reservation. Success reconciles with an independent five-second persistence token. Failure/cancellation can leave pending state because the provider may have processed the request; do not blindly release it. Preserve orphan charging, idempotency, consent, image access and cancellation semantics.

AiPromptProvider retains five-minute caching, active-version selection and existing fallbacks. Shared migrations remain central. Historical migration metadata is immutable; current model snapshot CLR names follow current owners without a schema change.

Focused application/domain/infrastructure/presentation tests live under Modules/Ai/tests. Central PostgreSQL suites cover quota concurrency, usage projections, prompt persistence and shared transaction composition; central HTTP suites cover routes and snapshots. No live provider calls are required. Test discovery is not execution evidence.

Architecture checks protect project layout, namespace alignment, module extraction, dependency references and controller convention discovery. See docs/adr/0038-read-model-composition.md and docs/ai/ai-persistence-boundary.md.
