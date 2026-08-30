# Wearables Module Guidelines

## Scope

Rules for `Modules/Wearables/`.

## Ownership

- Own provider connections, OAuth state, protected provider tokens, synchronization history, and daily wearable summaries.
- Keep Fitbit and future provider HTTP adapters/options in `FoodDiary.Integrations`; they implement module-owned ports.
- Keep the shared `FoodDiaryDbContext`, historical migrations, and model snapshot central.
- Preserve the legacy `FoodDiary.Application.Wearables` assembly and CLR/EF identities unless a separately reviewed migration changes them.

## Privacy and operations

- Treat access/refresh tokens, external provider user IDs, activity measurements, and sync history as sensitive health/credential data.
- Never log token values or OAuth state payloads. Keep token protection and OAuth-state validation inside module infrastructure.
- External provider calls must remain outside database/advisory-lock transactions.
- No recurring wearable sync exists today; adding one requires explicit JobManager ownership, idempotency, retry, privacy, and provider-rate-limit review.
