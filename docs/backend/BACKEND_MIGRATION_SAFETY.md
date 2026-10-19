# Backend Migration Safety

## Scope

This document defines how database migrations are validated and how deployment-time failures are handled.

## Current Deployment Path

- CI builds the Initializer image and pins its digest in `.github/workflows/deploy.yml`.
- Before uploading deployment configuration or stopping writers, deployment runs
  `scripts/database/check_primary_baseline_readiness.py` over the existing SSH
  connection. It reads the primary EF history in a read-only transaction. Empty
  databases and adopted histories without legacy entries pass; legacy history,
  database access errors and invalid responses block deployment. This preflight
  does not apply migrations or adopt history. A blocked deployment keeps the
  running application in place while the explicit cutover is prepared.
- Deploy stops the primary database writers, verifies a database backup, runs `docker compose --profile backend run -T --rm db-init update`, then starts the matching application images.
- The Initializer applies migrations through EF Core's `IMigrator`. The design-time context factory in [`FoodDiaryDbContextFactory.cs`](../../FoodDiary.Infrastructure/Persistence/FoodDiaryDbContextFactory.cs) also supports standalone migration tooling.

## Migration baseline (17 October 2026)

The primary database starts at `20261017130323_InitialBaseline`. It replaces the
185 migrations through `20261017003851_HardenTelegramAssertionRecovery`.
MailRelay and MailInbox have independent migration histories and are unaffected.
The baseline creates the complete current EF schema, the SQL-only billing
expression index, and the final reference data (5 roles, 10 daily advices,
10 email templates, 14 achievement definitions and the Telegram assertion policy).
Seed SQL is embedded in Infrastructure, so migration scripts and bundles include it.
Initial admin creation remains the Initializer's configured bootstrap operation.

### New databases

Run the usual Initializer `update`. No adoption step is needed. Future migrations
are added normally against `FoodDiaryDbContextModelSnapshot`.

### Existing databases: explicit one-time adoption

Do this before deploying the first application/Initializer image with the new
baseline. The regular deployment command does **not** perform adoption.

1. Use the **pre-baseline Initializer image** to finish the complete old chain,
   including the Telegram security cutover described below. The last repository
   revision containing that chain is `c8b050e39253a53c183d8076d0c19535e153151d`.
   Do not try to upgrade an older database using the new baseline migration.
2. Stop all database writers and verify a restorable backup, including the old
   `__EFMigrationsHistory`. Keep the old application/Initializer image digests.
3. With PostgreSQL connection settings supplied through the existing operational
   environment, run the reviewed script:

   ```sh
   psql -X -v ON_ERROR_STOP=1 -f scripts/database/Adopt-InitialBaseline.sql
   ```

   The script takes an exclusive history-table lock with a 10-second timeout.
   It verifies the exact 185 legacy IDs and the PostgreSQL 17/18 catalog fingerprint
   (columns/defaults, constraints, indexes, sequences and extensions). It then
   atomically replaces only the history rows with the baseline entry. It never
   executes schema operations, seed inserts, data backfills or the Telegram cutover.
   The only accepted additional history entry is the old data-only
   `20260208133129_SeedEmailTemplates`, which was removed from the original chain.
   PostgreSQL 18's separate NOT NULL constraint names are excluded from the catalog
   digest; column nullability is still checked directly.
   A database with missing/unknown migrations or schema drift is rejected without
   changing its history. Investigate the mismatch; do not bypass the checks.
4. Run the new Initializer `status`/`list`, then the normal deployment `update`.
   Confirm the baseline is applied and no migrations are pending before starting
   the matching application images. Repeating adoption on the unchanged baseline
   database is harmless; this dated script is not for databases with later migrations.

An ordinary migration update on an unadopted legacy database fails with the script
path before creating tables. Rolling back the baseline is explicitly unsupported:
it would otherwise drop the whole application schema, including an adopted
database. Recover with the verified backup and its matching images/history.
Do not start a pre-baseline migrator after adoption.

## Required Validation

Every schema-affecting backend change should preserve these checks:

1. Clean database migration
Apply the baseline and subsequent migrations to an empty PostgreSQL database and verify there are no pending migrations or uncaptured model changes.

2. Upgrade-path migration
Restore the frozen final legacy schema/reference-data fixture, add application rows,
adopt the baseline, and migrate to latest. Verify all application rows are unchanged.
Also verify incomplete/unknown history, schema drift and implicit adoption are rejected.

3. Bundle compatibility
Keep migration changes compatible with `dotnet ef migrations bundle` as a standalone tooling check. Production applies the same migration chain through the pinned Initializer image.

## Current Automated Coverage

- `MigrationSafetyIntegrationTests.CleanDatabase_MigrateToLatest_AppliesBaselineAndReferenceData`
- `MigrationSafetyIntegrationTests.LegacyDatabase_AdoptionPreservesEveryApplicationRowAndIsRepeatable`
- `MigrationSafetyIntegrationTests.CleanDatabase_ReferenceContentMatchesTheFrozenLegacyDatabase`
- The same suite covers refusal of incomplete/unknown history, schema drift,
  ordinary updates before adoption and baseline rollback.

These live in `Platform/tests/FoodDiary.Infrastructure.IntegrationTests/Integration`
and run against PostgreSQL via Testcontainers. `Fixtures/PreBaseline.sql` is a frozen
schema and seed-data result captured by applying the original chain to an empty
disposable PostgreSQL 17 database, not a production dump. Keep it independent of the
new baseline so compatibility checks can detect omissions. Removed historical
backfill/rollback tests are replaced by baseline adoption and current persistence
tests; earlier schema states must be upgraded with the pre-baseline image.

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
