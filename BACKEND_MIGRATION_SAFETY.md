# Backend Migration Safety

## Scope

This document defines how database migrations are validated and how deployment-time failures are handled.

## Current Deployment Path

- CI builds an EF Core migration bundle in `.github/workflows/deploy.yml`.
- Deploy runs the bundle on the server with `ConnectionStrings__DefaultConnection` and `FOODDIARY_CONNECTION_STRING` set from the production secret.
- The design-time context factory in [FoodDiaryDbContextFactory.cs](/C:/Users/alexi/OneDrive/Документы/GitHub/food-diary/FoodDiary.Infrastructure/Persistence/FoodDiaryDbContextFactory.cs) supports the same environment variables used by the bundle.

## Required Validation

Every schema-affecting backend change should preserve these checks:

1. Clean database migration
Apply the full migration chain to an empty PostgreSQL database and verify there are no pending migrations.

2. Upgrade-path migration
Apply an older migration state first, then migrate to latest and verify the upgrade completes without pending migrations.

3. Bundle compatibility
Keep migration changes compatible with `dotnet ef migrations bundle`, because deploy executes the bundle, not ad hoc CLI commands on the server.

## Current Automated Coverage

- `MigrationSafetyIntegrationTests.CleanDatabase_MigrateToLatest_AppliesFullMigrationChain`
- `MigrationSafetyIntegrationTests.DatabaseAtInitialCreate_CanUpgradeToLatest`

Both live in `tests/FoodDiary.Infrastructure.Tests/Integration` and run against PostgreSQL via Testcontainers when Docker is available.

## Release Checklist For Schema Changes

- Commit both migration files: `*.cs` and `*.Designer.cs`
- Verify `FoodDiaryDbContextModelSnapshot.cs` is updated correctly
- Run infrastructure tests
- Ensure deploy workflow still builds the migration bundle successfully
- Review destructive operations explicitly before merge

## Failure Handling

If migration execution fails during deploy:

1. Stop the rollout
Do not continue with application restart or health-check promotion until the database state is understood.

2. Inspect bundle logs
Use the deploy workflow log for the `Run migration bundle on server` step and identify the exact failing statement or migration.

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
