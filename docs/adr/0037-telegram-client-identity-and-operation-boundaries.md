# ADR 0037: Telegram client identity and operation boundaries

- Status: Accepted for implementation
- Date: 2026-09-12
- Owners: Identity, Users, Meals
- Related: [Telegram implementation plan](../plans/TELEGRAM_CLIENT_IMPLEMENTATION_PLAN.md), ADR 0030, ADR 0032
- Supersedes: None

## Context

FoodDiary is adding registration, login, explicit Telegram linking, photo autosave and undo. Telegram does not supply email. Existing User creation, JWT contracts and frontend guards assume an email address. Existing HTTP idempotency responses expire after 24 hours and cannot alone prevent duplicate meals after delayed recovery.

## Decision drivers

- Telegram-only users must be able to use the diary without a fictitious email.
- Existing email/password verification and provider identity uniqueness remain enforced.
- A transport client must not own nutritional rules or write another module's aggregates.
- Restart and ambiguous HTTP outcomes must not create duplicate diary entries.

## Considered options

1. Require email at Telegram onboarding: less migration work, but prevents the intended Telegram-only experience.
2. Store synthetic email: rejected because it misrepresents a contact/recovery channel.
3. Represent absent email explicitly and require it only for email-dependent features: selected.
4. Call AI and persist meals directly in the bot: rejected because it bypasses ownership, consent and quota rules.

## Decision

Users owns nullable email, Telegram identity uniqueness, time zone and last-login-method invariants. Email/password creation still requires an address. Adding a verified address requires an Identity-validated, single-use proof before invoking the Users capability. Neither email equality nor Telegram username automatically merges users.

Identity validates OIDC and Mini App proofs, consumes purpose-bound intents and issues/revokes sessions. OIDC subject and Telegram numeric ID are not assumed interchangeable. JWT identity remains UserId; email is an optional claim. Link changes advance SecurityVersion; disconnecting the last usable login is forbidden.

The separately deployed bot remains an HTTP transport client. Technical update receipts and workflow checkpoints are placed behind narrow Identity-owned integration ports; this placement must pass the existing dependency matrix before it is enabled. They store opaque foreign IDs, not foreign aggregates. Images and Ai retain their existing API/ownership/quota behavior. Meals owns atomic operation receipts, creation from a validated owned recognition result, and version-aware undo. No direct Ai-to-Meals write is introduced.

The implementation plan defines default deadlines and recovery behavior. Telegram reply delivery is separate from business completion: an ambiguous sendMessage result cannot cause a second Meal.

Terminal Telegram operations erase both protected input and checkpoint immediately after delivery completion or cancellation. Completed legacy content is erased in bot-scoped batches of at most 100 during work discovery. The bot/update identity and payload fingerprint remain for deduplication; clearing content must not re-admit old updates. Pending checkpoints remain available for recovery and are not silently expired while provider or business outcomes may be uncertain. User cancellation clears content from completed operations as well as fencing pending work. Final account purge invokes Identity's implementation of the existing Users.Contracts IUserDataPurgeParticipant extension point. It removes all of that user's operation rows, including deduplication metadata, in the coordinator's transaction; operations are never reassigned to the content successor. This direct contract dependency grants no access to foreign tables.

Meals serializes recognition mutations per UserId using a short PostgreSQL advisory-lock transaction. Creation flushes the new Meal through the shared unit of work to capture xmin, then saves the receipt before committing that same transaction. Failed attempts discard tracking and queued post-commit actions. Undo locks the owned Meal row before comparing the receipt version, preserving ordinary concurrent edit protection. External provider calls stay outside this transaction.

Creation uses the durable AI recognition ID as its permanent operation ID. Meals consumes only IFoodRecognitionResultReader from Ai Application.Abstractions, not its mutable job store. It checks for a prior receipt before reading the retained AI result, so expiration of AI history or deletion of the Meal cannot cause recreation. The normal CreateMeal handler and validator remain responsible for image ownership, nutrition and related events. The request timestamp is normalized to PostgreSQL microsecond precision before replay comparison.

## Consequences

HydrationOperationReceipt is an immutable operation outcome, not a many-to-many link. Its AmountMl, TimestampUtc and EntryId depend on the complete (UserId, OperationId) key: one user may issue multiple operations, and another user may reuse an operation identifier independently. The three retained facts are explicitly classified in the composite-key normalization guard. They remain after entry deletion so replay can reject a changed payload and avoid recreating a deleted entry. This does not authorize additional business snapshots in composite-key tables.

Hydration stages its entry and permanent owner/operation receipt in the existing command unit of work; one EF SaveChanges transaction commits both. Its repositories retain narrow DbSet injection and own no transaction. A duplicate-key race rolls back the losing entry; the durable bot retries a transient failure and reads the winning receipt. The receipt survives entry deletion and cascades with the user. Water uses the journal's immutable CreatedAtUtc, not retry time.

- User projections, email jobs, JWT refresh, billing guards and frontend need nullable-email coverage.
- After email-less users exist, old API builds requiring email cannot be rolled back blindly.
- Integration state and meal receipts need explicit retention and user-purge behavior.
- A proof of database concurrency is required; unit mocks are insufficient.

## Enforcement

Cross-module HTTP tests consume Ai.Application.Abstractions and Images.Infrastructure
directly to substitute only the external AI client and object storage. The real
recognition processor, quota store, image ownership checks and Meals flow remain
in the test host. These test dependencies do not change production module ownership.

TG-01 through TG-20 in the plan are the acceptance requirements, not claims of completed tests. Initial domain tests live in `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/Domain/TelegramAccountTests.cs`; further API, PostgreSQL and frontend evidence is recorded in the task workspace as implementation proceeds.

## Follow-up

Complete all implementation-plan phases and promote operational details into a runbook before enabling production registration/autosave.
