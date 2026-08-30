# Notifications extraction handoff

## Scope and compatibility

The worktree was created from local master `037bbc254b80b5e8c2cd683183fbec2cb4aa4315`, including its unpublished local history. No push or master modification was performed. See [ownership inventory](notifications-ownership-inventory.md) and [Wiki findings](notifications-module-extraction-wiki-findings.md).

Relocated `FoodDiary.Application.Notifications` into `Modules/Notifications/Application`, retaining its AssemblyName/RootNamespace. Added module Application.Abstractions, Domain, Infrastructure, PersistenceModel and three independent module test projects, plus dependency-free `Shared/FoodDiary.Outbox.Abstractions`. Removed the donor application project path and moved focused source/tests rather than retaining duplicates. All projects are included in their solution folders, dependency matrix, lockfiles and applicable Docker restore/source COPY sets.

Preserved command/query payloads, channels, localized text, routes/Swagger snapshots, recipient selection, provider deadlines, cancellation, retry/claim/replay behavior and EF schema. The owned VAPID/navigation validator methods were separated from the mixed provider validator without changing their bodies. No new outbox design or delivery guarantee is introduced.

Central seams: shared User/UserId and profile preferences; FoodDiaryDbContext, unit of work, historical migrations/snapshot; public legacy nested WebPushSubscriptionId converter; generic four-stream outbox engine/claimer/policy/replay; localized resource renderer; HTTP/SignalR presentation and scheduler/test-scheduler adapters; executable composition roots. The shared outbox interface now has no production project dependencies; email, image deletion, achievement evaluation and notification records all implement the unchanged contract.

## Verification

- Force-evaluate solution restore passed, with refreshed transitive locks.
- Full solution build at `.artifacts/notifications-final`: 0 warnings, 0 errors.
- Module tests: Application 111/111, Domain 22/22, Infrastructure/provider 76/76.
- ArchitectureTests: 748/748, including dependency matrix and Docker transitive copies.
- Central Application 1486/1486, Domain 1046/1046; Fasting consumer 167/167; Dietologist consumer 249/249.
- Presentation 825/825; JobManager 168/168; Web.Api unit 247/247.
- Notification HTTP integration 11/11; Swagger/payload contracts 16/16.
- Central Infrastructure: 758 passed, exactly six confirmed baseline failures (764 total).
- EF `migrations has-pending-model-changes` using central Infrastructure's design-time factory and isolated artifacts: no model changes. Existing EF tool/runtime version warning (10.0.10/10.0.11), not a model failure.
- Full PostgreSQL Infrastructure integration: 115/115, no skips (17m30s), including relational due/unlocked claiming and transactional image/web-push replay.
- Wiki update and verify: 7/7 stages passed; graph rebuilt over 7044 files / 31410 symbols; independent task scope validation found 0 out-of-scope paths. Architecture health: 501 production / 266 test edges, no enforced drift. NuGet vulnerability audit: no vulnerable packages reported. Staged C# whitespace verification passed (workspace-load warning only); `git diff --check` clean. Commit hook outcome is included with the final SHA in the task handoff.

Final TRX directory: `C:/Users/alexi/.codex/worktrees/421f/FD/.artifacts/notifications-results/` (one file named after each project; separate `FoodDiary.Web.Api.Contracts.trx`). Presentation TRX: `C:/Users/alexi/.codex/worktrees/421f/FD/tests/FoodDiary.Presentation.Api.Tests/TestResults/notifications-final.trx`.

Shared outbox source comparison against the base shows no changes to OutboxProcessingEngine, OutboxMessageClaimer, OutboxProcessingPolicy or OutboxDeadLetterReplayService. Central unit coverage includes caller cancellation during/after dispatch, safe timeout retry, due/unlocked claiming, stale replay rejection and multi-stream replay including web-push. Module tests retain failed-send retry and cancellation propagation, endpoint security and expired-subscription behavior. Semantic deduplication remains covered by the application/producer suites; this is not a claim of provider exactly-once delivery.

## Confirmed baseline failures

All six cases belong to `FoodDiary.Infrastructure.Tests.DependencyInjectionTests.AddInfrastructureAndFeatureModules_SplitRepositoriesResolveThroughSameScopedInstance(string primaryTypeName, string[] aliasTypeNames)`. The primary type below identifies each theory case. Every error is `System.InvalidOperationException`; exact messages are retained here:

| Primary type | Error message |
| --- | --- |
| `FoodDiary.Application.Abstractions.Billing.Common.IBillingPaymentRepository` | `Type 'FoodDiary.Application.Abstractions.Billing.Common.IBillingPaymentRepository' was not found.` |
| `FoodDiary.Application.Abstractions.Billing.Common.IBillingSubscriptionRepository` | `Type 'FoodDiary.Application.Abstractions.Billing.Common.IBillingSubscriptionRepository' was not found.` |
| `FoodDiary.Application.Abstractions.Billing.Common.IBillingWebhookEventRepository` | `Type 'FoodDiary.Application.Abstractions.Billing.Common.IBillingWebhookEventRepository' was not found.` |
| `FoodDiary.Application.Abstractions.OpenFoodFacts.Common.IOpenFoodFactsProductCacheRepository` | `Type 'FoodDiary.Application.Abstractions.OpenFoodFacts.Common.IOpenFoodFactsProductCacheRepository' was not found.` |
| `FoodDiary.Application.Abstractions.Wearables.Common.IWearableConnectionRepository` | `No service for type 'FoodDiary.Application.Abstractions.Wearables.Common.IWearableConnectionRepository' has been registered.` |
| `FoodDiary.Application.Abstractions.Wearables.Common.IWearableSyncRepository` | `No service for type 'FoodDiary.Application.Abstractions.Wearables.Common.IWearableSyncRepository' has been registered.` |

Baseline selected theory: 31 passed / 6 failed at the exact base commit. Comparing the sorted baseline/final TRX ErrorInfo.Message values yields six identical messages with no difference. These failures were deliberately not fixed in this extraction.

- Baseline TRX: `C:/Users/alexi/.codex/worktrees/421f/FD-notifications-baseline/tests/FoodDiary.Infrastructure.Tests/TestResults/baseline-di.trx`.
- Final TRX: `C:/Users/alexi/.codex/worktrees/421f/FD/.artifacts/notifications-results/FoodDiary.Infrastructure.Tests.trx`.
- Additional detached baseline worktree retained for later safe cleanup: `C:/Users/alexi/.codex/worktrees/421f/FD-notifications-baseline`, with its `.artifacts/notifications-baseline` build outputs. Neither worktree nor its artifacts were cleaned.

## Integration risks

Likely merge conflicts are central solution/Docker COPY lists, project-reference matrix and exact composition guardrails, central Application.Abstractions/Infrastructure references, DbContext explicit registrations, host composition, shared AssemblyInfo friend declarations, AGENTS/module map/ownership manifest, generated Wiki indexes/review ledger and transitive NuGet locks. Resolve by preserving both modules' registrations/references and regenerating locks/indexes, not by dropping one side. No migration/snapshot or HTTP contract changes should be required.
