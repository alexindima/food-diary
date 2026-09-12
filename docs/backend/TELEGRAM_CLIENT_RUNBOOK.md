# Telegram client operations

Status: preparation for release. This document does not authorize deployment or claim that the TG-01–TG-20 acceptance matrix is complete.

The separate [Telegram test stand](TELEGRAM_TEST_STAND.md) uses a standalone Compose file. Its current limitations and pending live checks are recorded there; do not use production Compose configuration as the test environment.

## Configuration

The API and `telegram-bot` services read `.env` in the current root Compose file. Keep secret values outside source control. Do not print resolved Compose configuration containing secrets in CI logs.

| Environment variable | Purpose |
| --- | --- |
| `TelegramClient__LoginEnabled` | Enables the new Telegram login flow; default false. |
| `TelegramClient__RegistrationEnabled` | Enables account creation; requires login enabled. |
| `TelegramClient__OperationsEnabled` | Enables the durable operation API. |
| `TelegramClient__BotId` | Positive numeric ID matching the prefix of the bot token when operations are enabled. |
| `TelegramAuth__BotToken` | API-side verification of Mini App assertions. |
| `TelegramBot__Token` | Token used by the separately running bot; use the same bot identity. |
| `TelegramBot__ApiSecret` | Shared API/bot authentication secret, at least 16 characters. |
| `TelegramBot__ApiBaseUrl` | API base origin, for example `http://api:5000` inside Compose. |
| `TelegramBot__WebAppUrl` | Public HTTPS application URL without query, fragment or user information. |
| `TelegramBot__OperationsEnabled` | Switches the bot to durable operation processing. |
| `TelegramOidc__Enabled` | Enables browser OIDC login. |
| `TelegramOidc__ClientId` | Numeric Telegram OIDC client ID, matching the bot ID in `TelegramAuth__BotToken`. Startup validation rejects a mismatch. |
| `TelegramOidc__ClientSecret` | OIDC client credential. |
| `TelegramOidc__RedirectUri` | Registered public HTTPS callback, ending in `/auth/telegram/callback`. |

Register the exact callback and application domain with Telegram. Configure the bot's Mini App to open the same application. The bot runs with the Compose `full` profile and depends on API readiness. Do not run two long-polling receivers for the same bot token during replacement.

## Release order

1. Complete the implementation plan acceptance matrix and record the actual tested revision. Test with a separate bot and HTTPS environment before enabling production registration.
2. Back up PostgreSQL and the API Data Protection key ring. Verify restoration in an isolated environment. Review all pending migrations, including unrelated changes in the release.
3. Keep the new login, registration and operation switches disabled while applying migrations and deploying the compatible API, frontend and bot builds. Use the repository's normal signed-image deployment process and initializer; do not create a second ad hoc migration path.
4. Verify API readiness, startup option validation, callback registration and persisted key-ring access. Confirm that ordinary email/password and Google accounts still work.
5. Enable Telegram login and validate an existing linked account. Enable registration only after verifying the email-less profile, token refresh, backup login and admin views.
6. Enable both the server and bot operation switches. Exercise one photo, one retry, undo, water, today and seven-day summaries with a test account. Confirm the records in the web diary and use its selected time zone when comparing dates.

## Recovery and rollback

The API stores protected Telegram intents and operation payloads. Compose mounts `api-data-protection-keys` at `/var/lib/fooddiary/data-protection-keys`; API configuration points `DataProtection__KeyRingPath` there. Retain this volume across container recreation. A database backup alone does not restore decryptability.

On key loss, preserve the database and restore the original key ring. Do not clear operation rows or create new operation IDs to bypass decryption errors. The PostgreSQL recovery test verifies that work can resume with the restored key; it does not establish an operational backup procedure by itself.

For an incident, stop the bot receiver/worker first, then disable the affected feature switches in the API. Preserve operation checkpoints and permanent meal/hydration receipts. Stopping the worker pauses pending work; it is not cancellation of work already committed. Inspect the diary before requesting another photo.

After email-less users have been created, do not roll back to a build requiring non-null email. Prefer a compatible roll-forward or a compatible previous build with feature switches disabled. Do not reverse nullable-email migrations or delete Telegram-only users to make an old build start.

A successful unlink revokes the identity generation and schedules operation cancellation after account persistence. The post-commit cancellation is not a durable outbox: if the process stops in that interval, subsequent operation authorization still fences the old generation. Independent retention and account-purge completion must be verified before release.

## Failure investigation

- Distinguish business completion from Telegram message delivery. A lost `sendMessage` response can leave an uncertain notification outcome; it must not trigger another business operation.
- Correlate by operation ID and recognition ID. Never include bot tokens, bearer tokens, Mini App assertions, email verification links, photos or protected payloads in diagnostic output.
- A repeated save uses the same recognition ID and original message time. A changed timestamp is a conflict. After undo/deletion the receipt prevents recreation.
- For AI consent or quota errors, use the existing application consent and entitlement flow. Do not bypass it in the bot or rerun real provider calls as an automated smoke test.
- Verify the original owner's diary when resolving uncertainty. Do not infer successful delivery from a stored meal, or successful meal creation from a Telegram acknowledgement.

## Evidence and remaining gates

See [implementation plan](../plans/TELEGRAM_CLIENT_IMPLEMENTATION_PLAN.md) and [ADR 0037](../adr/0037-telegram-client-identity-and-operation-boundaries.md). Local verification evidence is collected under `.artifacts/telegram-client/`; mocks are labelled separately from real provider checks. Production enablement still requires the full acceptance audit, lifecycle/retention checks and an actual Telegram smoke test.
