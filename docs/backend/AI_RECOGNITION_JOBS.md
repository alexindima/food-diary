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

## Rollout and verification

Apply `AddFoodRecognitionJobs` through the normal migration deployment, then deploy the API and JobManager before enabling the new frontend. Old frontend/API consumers continue using the existing endpoints. No new AWS resources or external queue are required. Rolling back the frontend leaves accepted jobs available for processing; retain the table and worker while draining pending work.

Focused tests cover concurrent PostgreSQL admission/claim, ownership isolation, timeout fencing, partial checkpoints and image retention; mocked AI processor tests cover ordering and no automatic replay; frontend tests cover lost POST responses, HTTP fallback, SignalR hints, recovery, terminal failure, and user changes. Tests must not invoke the live AI provider.
