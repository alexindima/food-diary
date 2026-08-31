# Ai ownership inventory

Base: a11d9a5d2c4dce2682abb4691b2b23fb6b38b9a6. Worktree: C:/Users/alexi/.codex/worktrees/901d/FD. Initial HEAD equals local master; no pre-existing changes.

## Proven responsibility and intended physical boundary

- Application: AnalyzeFoodImage, ParseFoodText, CalculateFoodNutrition and GetUserAiUsageSummary slices; OpenAiFoodService quota orchestration, AiUserContextService, prompt administration, administration/usage projections and ApplicationAiTelemetry. Preserve FoodDiary.Application.Ai assembly and CLR namespaces.
- Application/Abstractions: existing Ai provider, quota and repository ports, models and errors. Move the four AiUsage* projection records from Admin/Models with their CLR namespace preserved: these describe Ai-owned usage, not Admin ownership.
- Domain: AiUsage/AiPromptTemplate and their IDs have no central inverse navigation and can depend one-way on central UserId. No invented events or empty layer.
- Infrastructure/Model: four Ai configurations and internal quota ledger state (AiQuotaPeriod, AiQuotaReservation, AiQuotaReservationState). These persistence types currently consume the reservation request port; keep them in the persistence model, not a Domain-to-Application dependency. Preserve internal visibility using specific friend assemblies rather than widening public API.
- Infrastructure: AiUsageRepository, AiPromptTemplateRepository, AiQuotaRepository, AiPromptProvider and complete module registration. Central context explicitly applies the model. Application registration remains usable independently of persistence.
- Semantic administration interfaces currently live in Application/Common and expose existing aggregates/projections. Keep this compatible surface for Admin; do not invent new Contracts or redesign return types solely for extraction.

## Runtime and safety evidence

OpenAiFoodService checks profile/consent before prompt lookup, provider budget calculation and ReserveAsync. Provider I/O occurs after reservation's independent database transaction. Success uses an independent five-second persistence token to reconcile actual usage (or conservative budget estimate). Provider failure/cancellation after reserve intentionally leaves pending state: no blind release, because processing may have occurred. Expiry charges pending reservations as orphaned; late success reconciles the estimate. Explicit ReleaseAsync is idempotent for non-pending state. Reserve serializes the user/month period with PostgreSQL FOR UPDATE; reconcile updates period, reservation and AiUsage in one transaction. Preserve existing lock order, retry strategy and all limits/timeouts.

AiPromptProvider owns a singleton five-minute memory cache with scoped DB lookup, latest active version selection and unchanged fallback texts; administration currently has no immediate cache invalidation. Do not silently change this consistency policy. Preserve metric names/tags and sensitive-data restrictions.

## Central and external compatibility seams

- User, UserId, UserAiQuotaState, UserAiTokenLimitUpdate and UserAdminAiQuotaUpdate remain central: User directly owns profile fields and calls their invariants. Ai owns quota enforcement/ledger, while IUserAiProfileReadService supplies limits and consent. Consent acceptance remains Users lifecycle behavior. No central-to-module Domain back edge.
- Shared FoodDiaryDbContext, all DbSets, historical migrations/snapshot and user-deletion orchestration remain central. Internal quota DbSets require explicit friend access across the model/adapter seam.
- InfrastructureTelemetry is a shared meter with existing quota-orphan instrumentation; retain its identity via narrow internal access rather than duplicate meters.
- OpenAI HTTP client, SDK/transport options, parsing and retry/timeout/provider configuration remain FoodDiary.Integrations. No provider endpoint, model, price, prompt or actual external request changes.
- MealAiSession/MealAiItem, IDs and enums remain Meals-owned. Image access/storage validation remains Images-owned. HTTP transport and host policy remain Presentation/Web.Api; composition roots alone acquire the persistence module.
- Admin consumes IAiPromptAdministrationService and IAiAdministrationReadService; its own handlers/mappings remain Admin. Mixed Admin feature tests must stay with their owner.

## Tests and consumers

Module-owned: tests/FoodDiary.Application.Tests/Ai; AiUsageInvariantTests and AiPromptTemplateInvariantTests; Infrastructure unit AiQuotaRepositoryTests, AiPromptProviderTests and mocked OpenAiFoodServiceTests. Move focused suites into actual module test projects without duplication.

Central/mixed: Users/AiConsentTests, Admin feature tests, Meals AI invariants, Infrastructure DI/telemetry tests, Presentation AiFoodController/AiHttpMappings/UserAiConsent tests and Web.Api HTTP suites. PostgreSQL AiQuotaRepositoryIntegrationTests and AiUsageRepositoryIntegrationTests share the central database fixture; retain that ownership and execute the entire central Infrastructure.IntegrationTests once, without a filter. These cover twenty-request concurrency, duplicate requests, reconciliation, usage aggregation and prompt persistence. Test discovery is not execution evidence.

Initial Wiki manifest incorrectly calls Ai an orchestrator with no owned entities. Source proves two domain entities plus persistent quota ledger. Its persistence hint also needs verification against the actual Persistence/Configurations/Ai path.
