# Wearables Module Guidelines

## Scope

Rules for `Modules/Wearables/`.

## Ownership

- Own provider connections, OAuth state, protected provider tokens, synchronization history, and daily wearable summaries.
- Own Fitbit HTTP adapters/options in Infrastructure/Providers. Hosts call AddWearablesProvider explicitly; shared HTTP/URI helpers remain in Integrations through a one-way reference.
- Keep the shared `FoodDiaryDbContext`, historical migrations, and model snapshot central.
- Preserve the legacy `FoodDiary.Application.Wearables` assembly and CLR/EF identities unless a separately reviewed migration changes them.

## Privacy and operations

- Treat access/refresh tokens, external provider user IDs, activity measurements, and sync history as sensitive health/credential data.
- Never log token values or OAuth state payloads. Keep token protection and OAuth-state validation inside module infrastructure.
- External provider calls must remain outside database/advisory-lock transactions.
- No recurring wearable sync exists today; adding one requires explicit JobManager ownership, idempotency, retry, privacy, and provider-rate-limit review.

## Error ownership

Feature error factories belong to their existing owner contracts; call them directly.
The corresponding central Errors facades are retired. Preserve exact codes, messages,
kinds and parameter formatting. Reference the owner explicitly; this grants no foreign
repository or aggregate capability. See docs/ai/feature-error-retirement.md.
