# Backend Migration Safety

## Scope

This document defines how database migrations are validated and how deployment-time failures are handled.

## Current Deployment Path

- CI builds the Initializer image and pins its digest in `.github/workflows/deploy.yml`.
- Deploy stops the primary database writers, verifies a database backup, runs `docker compose --profile backend run -T --rm db-init update`, then starts the matching application images.
- The Initializer applies migrations through EF Core's `IMigrator`. The design-time context factory in [`FoodDiaryDbContextFactory.cs`](../../FoodDiary.Infrastructure/Persistence/FoodDiaryDbContextFactory.cs) also supports standalone migration tooling.

## Required Validation

Every schema-affecting backend change should preserve these checks:

1. Clean database migration
Apply the full migration chain to an empty PostgreSQL database and verify there are no pending migrations.

2. Upgrade-path migration
Apply an older migration state first, then migrate to latest and verify the upgrade completes without pending migrations.

3. Bundle compatibility
Keep migration changes compatible with `dotnet ef migrations bundle` as a standalone tooling check. Production applies the same migration chain through the pinned Initializer image.

## Current Automated Coverage

- `MigrationSafetyIntegrationTests.CleanDatabase_MigrateToLatest_AppliesFullMigrationChain`
- `MigrationSafetyIntegrationTests.DatabaseAtInitialCreate_CanUpgradeToLatest`

Both live in `Platform/tests/FoodDiary.Infrastructure.IntegrationTests/Integration` and run against PostgreSQL via Testcontainers when Docker is available.

## Release Checklist For Schema Changes

- Commit both migration files: `*.cs` and `*.Designer.cs`
- Verify `FoodDiaryDbContextModelSnapshot.cs` is updated correctly
- Run infrastructure tests
- Verify migration bundle compatibility and the Initializer build
- Review destructive operations explicitly before merge

## Failure Handling

### Telegram security cutover

`HardenTelegramAssertionRecovery` is a coordinated authentication cutover.
Stop old API/authentication writers before applying it and start the matching
application revision afterwards. Existing JWT/refresh sessions retain their
normal behavior; Telegram Mini App/widget assertions issued before the persisted
cutover must be refreshed. Allow a five-minute assertion-login maintenance window:
the cutover includes the old validator's five-minute future clock allowance so
previously consumed future-dated proofs cannot become reusable later. Password,
Google and existing sessions do not use this assertion gate. The database rejects the old raw-fingerprint writer,
so a mixed-version rollout fails closed for those authentication endpoints.

The migration removes transient consumed-assertion markers, stores a single
`TelegramAssertionPolicies.NotBeforeUtc` boundary, and requires versioned
canonical fingerprints. All new validators and replay consumers must ship
together. Do not roll this migration back with Telegram login enabled: an old
binary cannot enforce the cutover. Disable those assertion endpoints until their
maximum configured lifetime has elapsed if an older authentication build must
be restored.

`Users.TelegramLinkVerifiedForRecovery` records trustworthy attachment
provenance. New links require an already confirmed email, while Telegram-first
accounts retain their primary sign-in method. Existing email/password links have
unknown provenance and remain untrusted until the owner explicitly relinks with
confirmed email and fresh Telegram proof. Password recovery removes such links,
including their OIDC binding, even if email was verified after attachment.
Legacy Telegram-first/passwordless accounts are conservatively preserved by
the migration's explicit backfill. No production data is changed by local test
execution; deploy the migration through the normal reviewed Initializer path.

If migration execution fails during deploy:

1. Stop the rollout
Do not continue with application restart or health-check promotion until the database state is understood.

2. Inspect Initializer logs
Use the deploy workflow log for the `db-init update` invocation and identify the exact failing statement or migration.

3. Determine database state
Check `__EFMigrationsHistory` on the target database to confirm whether the failing migration was applied partially, not applied, or completed before a later step failed.

4. Choose the recovery path deliberately
- If no migration was applied, fix the migration and redeploy.
- If the migration completed but the app failed later, do not roll back schema blindly; fix the application/deploy issue and redeploy.
- If a migration failed mid-flight, create an explicit corrective migration or a reviewed manual SQL fix. Do not edit old committed migrations in place after they have been deployed.

5. Document the incident
Record the failing migration, affected environment, recovery action, and whether a permanent runbook update is needed.

## Current Deferred Items

- No automated rollback rehearsal yet.
- No seeded large-data migration performance baseline yet.
- No automated verification of the built migration bundle in tests yet.
