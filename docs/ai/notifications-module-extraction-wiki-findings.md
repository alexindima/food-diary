# Notifications module extraction: Wiki findings

## Useful evidence

- `start` selected the critical governed profile and correctly identified application extraction, composition-root registration, persistence compatibility, background-job cancellation/retry safety, consumer compilation and architecture checks as obligations.
- `research` found relevant extraction precedents and the scoped Notifications guide.
- `privacy` identified web-push user-agent metadata, notification preference health signals and password-presence context requiring boundary review.
- `topology` found the web-push HTTP adapter, endpoint network policy, fasting-notification schedule and notification-cleanup schedule, while explicitly warning that declarations do not prove runtime DNS, proxy, retry or idempotency behavior.

## False positives and negatives

- Initial `start` grounded 61 planned paths primarily under Billing for a Notifications extraction. Those paths were unrelated and were not used as implementation authority.
- `brief` and `test-plan` returned zero-risk/zero-test output when called without repeated intent/path arguments, even though a current governed workspace existed.
- `journeys` returned no matches despite the notification feed, preferences and push-delivery user journeys being present in controllers and integration tests.
- `topology` found only fasting and cleanup registrations; the notification web-push outbox consumer and its claim/retry/dead-letter behavior required direct code inspection.
- `trace` attempted a full graph build and failed because TypeScript compiler prerequisites were absent, although the requested flow was backend-only.

## Reproducible tooling defects

1. A module-extraction `start` can select planned paths from an unrelated recent extraction precedent instead of the named module.
2. Workspace-aware `brief` and `test-plan` do not inherit current task intent/planned paths and silently produce empty guidance.
3. A backend-only `trace` can require frontend TypeScript extraction and abort instead of degrading to the available JSON/backend graph.
4. Read-only JSON fallback repeatedly reports missing TypeScript prerequisites but does not surface one consolidated remediation at workflow start.

These are general routing/context defects; no Notifications-specific generator exception should be added.

## Final workflow observations

- With explicit module/shared/context planned paths, `brief`/`test-plan` identified persistence, cancellation, concurrency and model checks, but their first 12 focused files omitted the three Notifications module suites and included unrelated repository suites. The independent ownership inventory supplied the actual module/provider/producer coverage.
- After installing the repository's existing frontend dependencies, backend `trace -Query DeliverTestNotification -TraceView Backend` succeeded through semantic fallback and linked the relocated handler, writer, refresh service, shared post-commit queue/unit of work and focused test. The initial missing-compiler failure is an environment prerequisite coupled too broadly to backend navigation, not missing backend implementation.
- `delivery-replan` failed atomically with `compiled-index-projection-missing` and requested `graph-build`; no partial governed-state edits were accepted. A graph build correctly rejected concurrent worktree changes; that concurrency guard is expected behavior, not a module-specific defect.
- Full updated Wiki verification passed all seven selected stages, including indexes, change policy and source impact. Generated navigation does not substitute for executed test or EF evidence.
- After the stable graph build (7044 files, 31410 symbols), `delivery-replan` succeeded and generated a 323-path manifest. The separate inventory-scoped task contract verified 0 out-of-scope paths. Acceptance/evidence initially remained pending until explicit test/model/review results were recorded; this is expected governance behavior, not a test failure.
- The evidence API accepts additional manual check IDs, but acceptance mapping rejects those IDs and delivery lineage marks them inactive even when recorded successfully. Reproduction: add a named focused-module check with `evidence-check`, then try `acceptance-map -CheckId` or `delivery-validate`. Keep concrete module results in the durable handoff and map acceptance through the active compiled check/review IDs; do not add a module-specific generator special-case.

## Confirmed baseline failures

The selected `AddInfrastructureAndFeatureModules_SplitRepositoriesResolveThroughSameScopedInstance` theory was executed at exact original commit `037bbc254b` in a separate detached worktree. It produced 31 passes and the same six failures as the first relocation: OpenFoodFacts cache repository type lookup; Billing subscription/payment/webhook repository type lookup; Wearables connection/sync repository registration. The messages matched, not merely the test names. The baseline TRX remains in `FD-notifications-baseline/tests/FoodDiary.Infrastructure.Tests/TestResults/baseline-di.trx` beside this worktree. No master changes or artifact cleanup were performed.
