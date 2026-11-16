# FoodDiary.Initializer

Thin console host for operational database tasks.

## Commands

```bash
dotnet run --project FoodDiary.Initializer -- status
dotnet run --project FoodDiary.Initializer -- list
dotnet run --project FoodDiary.Initializer -- update
dotnet run --project FoodDiary.Initializer -- update 20261017130323_InitialBaseline
dotnet run --project FoodDiary.Initializer -- rollback-last
dotnet run --project FoodDiary.Initializer -- seed-usda ./usda-data
```

## Configuration

The runner resolves `ConnectionStrings:DefaultConnection` from these sources:

- `FoodDiary.Web.Api/appsettings.json`
- `FoodDiary.Web.Api/appsettings.{Environment}.json`
- Development user secrets
- ordinary environment variables such as `ConnectionStrings__DefaultConnection`
- `FOODDIARY_ConnectionStrings__DefaultConnection`
- `--connection-string "..."`

The last source wins.

Task initialization uses only its validated Development task file and an optional
explicit `--connection-string`; normal environment and repository settings do not
participate in that isolated configuration.

Running `update` without a target migration also bootstraps the initial administrator after migrations complete.
Configure it with `InitialAdmin__Email` and `InitialAdmin__Password`. The bootstrap is idempotent and skips
creation when the password is absent or the user already exists. `InitialAdmin__BootstrapTimeoutSeconds`
controls its timeout and defaults to 30 seconds.

## USDA reference data

`seed-usda` loads the extracted FoodData Central SR Legacy CSV files into the USDA reference tables.
The entire load commits together. After a timeout or other import failure, rerun the same command.
It also rebuilds a partial import left by an older Initializer. A completed import is skipped.
Use `--force` to refresh a completed import, including one that still has category codes instead
of category names. If a replacement fails, the previous reference data remains intact.
USDA import commands use a 120-second timeout unless the connection string sets `Command Timeout`.

## Public USDA catalog

After loading the USDA reference data, preview and import the curated Russian staples:

```powershell
dotnet run --project FoodDiary.Initializer -- import-usda-catalog ./scripts/database/usda-staples-ru.json --dry-run
dotnet run --project FoodDiary.Initializer -- import-usda-catalog ./scripts/database/usda-staples-ru.json
```

The same version 1 manifest works on any database seeded with these USDA records. Each entry
specifies the FDC ID, Russian name, product type and Russian category. The command reads
nutrition per 100 g and the original English name from the local FoodData Central data.
Each imported item requires energy, protein, fat, carbohydrate, fiber and alcohol measurements.
Imported records are public and belong to the system content owner. Re-running skips an existing
public product linked to the same FDC ID. The import adds records in one transaction.

## Notes

- EF Core migrations stay in `FoodDiary.Infrastructure` next to `FoodDiaryDbContext`.
- `FoodDiary.Initializer` only executes operational commands.
- `rollback-last` is a convenience wrapper around migrating to the previous applied migration.

## Migration history baseline

The primary database uses `20261017130323_InitialBaseline`. New databases use the
normal `update` command. Existing databases must first finish the old migration
chain and explicitly run `scripts/database/Adopt-InitialBaseline.sql` before the
first deployment of the new Initializer. Ordinary `update` does not adopt old
history automatically, and rolling back this baseline is unsupported.
See [the migration runbook](../docs/backend/BACKEND_MIGRATION_SAFETY.md) for the
required backup, stopped-writer cutover, checks and recovery procedure.
