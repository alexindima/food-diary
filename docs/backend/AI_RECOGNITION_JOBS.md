# Durable photo recognition

Photo recognition started from the meal dialog, product dialog, or shared AI input is a persisted background operation. The existing synchronous vision, text, and nutrition endpoints remain compatible; edited-item nutrition and text recognition still use their existing HTTP flows.

## Request and recovery

- `POST /api/v1/ai/food/recognitions` accepts a client-generated UUID, owned image asset ID, and optional description. Premium access, AI consent, ownership, request validation, and rate limiting apply before admission. It returns HTTP 202 with the job. At most two queued/running jobs per user are admitted.
- Repeating the same UUID and payload returns the existing job; changing the payload returns HTTP 409. A short database transaction serializes task IDs and per-user admission.
- `GET /api/v1/ai/food/recognitions/{id}` and collection GET return only the authenticated user's data. Recovery does not require an active premium subscription. The history displays the latest ten tasks created within seven days.
- The browser persists its pending UUID before POST and retains it when acceptance is uncertain or the view closes. Retrying a lost POST response uses that UUID. History recovery uses GET only.
- `/hubs/food-recognition` sends `RecognitionChanged` with only a task ID. HTTP polling every five seconds remains active, including when SignalR cannot connect. Reconnect triggers a refresh; a changed signed-in user cannot receive the previous user's result in the UI.

## Execution and paid calls

`FoodRecognitionWorker` runs in JobManager. PostgreSQL `FOR UPDATE SKIP LOCKED` claims one queued row in a short transaction and commits `Running` before calling AI. Separate worker replicas can claim independently. No external provider call is enclosed in the database retry strategy.

The worker reuses the existing AI command handlers, consent checks, quota reservations, provider timeout, and usage reconciliation. Vision and nutrition have distinct deterministic quota request IDs derived from the job UUID. Vision is persisted before nutrition begins. A nutrition failure preserves the recognized items and exposes a separate error code. Opening a completed result does not calculate nutrition again.

Running jobs without a checkpoint for five minutes become `Failed` with `Ai.RecognitionInterrupted`. They are never automatically replayed: after a lost provider response or process restart, the system cannot know whether a paid call completed. The user can explicitly start a new attempt. A late worker cannot overwrite an expired terminal job or begin nutrition after losing the vision checkpoint fence. Queued jobs expire after one day.

## Data lifecycle and delivery

Jobs store image reference/URL, description, recognized items, nutrition, status, and safe error codes; no provider credentials or raw provider failure bodies are persisted. Terminal records expire seven days after their last update. The image FK uses `ClientNoAction`, preventing image deletion while a job retains the reference. Ai's user purge participant removes jobs before the Images participant runs; user deletion also has a cascading FK.

Each API instance checks recently updated rows every two seconds and sends hints to its own connected users. This does not require a SignalR backplane. Hints can be duplicated or missed; persisted owner-scoped GET responses are authoritative. The worker must be running for processing and retention cleanup.

## Owner rules and executable examples

The five application patterns in [Application patterns](../architecture/APPLICATION_PATTERNS.md)
are applied together here. `FoodRecognitionLifecycle` is the pure transition
owner; `FoodRecognitionPolicy` owns provider value rules and second-stage
selection. The handler and PostgreSQL store retain the side effects and atomic
source-state fences. `FoodRecognitionResult` decodes the existing job DTO into
`Pending`, `Unusable` or a privately constructed `Ready` with required vision and
nutrition. Ownership checks precede that decoding.

| Source | Action | Target |
| --- | --- | --- |
| Queued | Claim | Running |
| Queued | Interrupt after queue expiry | Failed |
| Running | Checkpoint vision | Running |
| Running | Complete, including vision-only or nutrition failure | Succeeded |
| Running | Vision failure or interrupt | Failed |
| Succeeded, Failed, unknown | Any worker action | Rejected |

Persisted and HTTP status strings remain `Queued`, `Running`, `Succeeded` and
`Failed`. `Succeeded` still means vision completed; it does not promise usable
nutrition. Nutrition failure keeps its separate error code and the vision
checkpoint. None of these transitions adds automatic paid-call replay.

The provider decoder rejects nonpositive item amounts, negative nutrients or
totals, probabilities/coordinates outside 0–1, and nutrition item counts that
differ from the request. It retains fractional amounts, their declared units,
zero nutrients, optional coordinates and decimal precision. Nutrition represents
totals for the supplied item amounts; product-label nutrition retains its
separate declared basis and nullable unreadable fields. The decoder does not
convert units, round values or derive calories from macros.

| Example | Expected outcome | Executable evidence |
| --- | --- | --- |
| Queued job has not been claimed | No completion or vision checkpoint | `FoodRecognitionLifecycleTests.UnclaimedCompletionAndRunningReplay_AreForbidden` |
| Terminal or unknown job receives a late worker action | No transition or replay | `FoodRecognitionLifecycleTests.TerminalAndUnknownStates_RejectEveryWorkerAction`; `FoodRecognitionJobStoreIntegrationTests.InterruptedTask_PreservesCheckpointRejectsLateCompletionAndEventuallyReleasesImage` |
| Product label or empty vision | No nutrition provider call | `ProcessNextFoodRecognitionCommandHandlerTests.ProductLabel_PersistsDirectResultAndSkipsEstimatedNutrition`; `ProcessNextAsync_EmptyVisionCompletesWithoutChargingForNutrition` |
| Nutrition fails after the vision checkpoint | Retain vision and the separate nutrition error; reject a late success | `ProcessNextFoodRecognitionCommandHandlerTests.ProcessNextAsync_NutritionFailurePreservesVisionAndDoesNotRepeatIt`; `FoodRecognitionJobStoreIntegrationTests.NutritionFailure_PreservesCheckpointAndRejectsLateSuccess` |
| Status says Succeeded but a payload is absent or inconsistent | `Ai.RecognitionNotUsable`, never Ready | `FoodRecognitionResultReaderTests.GetCompletedAsync_InconsistentPersistedSuccessCannotBecomeReady`; `GetCompletedAsync_RejectsMissingNutritionForRecognizedItem` |
| Another owner requests a complete result | `Ai.RecognitionNotFound` | `FoodRecognitionResultReaderTests.GetCompletedAsync_DoesNotExposeAnotherUsersResult` |
| Provider supplies negative nutrition, zero amount, invalid confidence or missing/extra nutrition items | `Ai.InvalidResponse`, one provider request | `ProviderRecognitionContractTests.InvalidProviderValues_AreRejectedWithoutFallbackOrAnotherPaidCall` |
| Provider supplies 0.125 pcs, confidence 0 or 1, optional coordinates | Preserve amount, unit and probability boundaries | `ProviderRecognitionContractTests.ValidVision_RetainsFractionalQuantityUnitAndProbabilityBoundaries` |
| Provider supplies 52.125 calories with zero macros | Preserve 52.125 and zeros | `ProviderRecognitionContractTests.Nutrition_RetainsZerosPrecisionAndProviderTotalsWithoutRecalculatingFromMacros` |

## Rollout and verification

Apply `AddFoodRecognitionJobs` through the normal migration deployment, then deploy the API and JobManager before enabling the new frontend. Old frontend/API consumers continue using the existing endpoints. No new AWS resources or external queue are required. Rolling back the frontend leaves accepted jobs available for processing; retain the table and worker while draining pending work.

Focused tests cover concurrent PostgreSQL admission/claim, ownership isolation, timeout fencing, partial checkpoints and image retention; mocked AI processor tests cover ordering and no automatic replay; frontend tests cover lost POST responses, HTTP fallback, SignalR hints, recovery, terminal failure, and user changes. Tests must not invoke the live AI provider.
