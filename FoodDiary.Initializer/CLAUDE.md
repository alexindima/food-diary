# FoodDiary.Initializer

Standalone CLI for EF Core database migration management. Runs a single command and exits.

## Commands

| Command | Description |
|---------|-------------|
| `list` | Enumerate all migrations, mark as applied/pending |
| `status` | Show connectivity, applied/pending counts, current migration |
| `update [target]` | Apply migrations up to target or latest |
| `rollback <target>` | Roll back to specific migration |
| `rollback-last` | Roll back to second-to-last applied migration |

## CLI Usage

```bash
dotnet run --project FoodDiary.Initializer -- <command> [target] [--connection-string|-c <conn>]
```

## Configuration Sources (in order)

1. Default host builder (env vars, command-line)
2. `FOODDIARY_*` environment variables
3. Sibling `FoodDiary.Web.Api/appsettings.json` (if exists)
4. `--connection-string` CLI override

Exits with code 1 if `DefaultConnection` is not found.

## Architecture

- Reuses `AddApplication()` + `AddInfrastructure()` DI from the shared layers
- Obtains `FoodDiaryDbContext` from DI
- Uses EF Core `IMigrator` and `IMigrationsAssembly` directly
- Single-file implementation (`Program.cs`)
- Hand-rolled CLI parser (`InitializerCommand` record)

## Dependencies

- Project references: `FoodDiary.Application`, `FoodDiary.Infrastructure`
- NuGet: `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Relational`, `Microsoft.Extensions.Hosting`
