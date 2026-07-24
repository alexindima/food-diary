# FoodDiary.Initializer

Thin console host for operational database tasks.

## Commands

```bash
dotnet run --project FoodDiary.Initializer -- status
dotnet run --project FoodDiary.Initializer -- list
dotnet run --project FoodDiary.Initializer -- update
dotnet run --project FoodDiary.Initializer -- update 20260209005246_AddShoppingLists
dotnet run --project FoodDiary.Initializer -- rollback-last
dotnet run --project FoodDiary.Initializer -- rollback 0
```

## Configuration

The runner resolves `ConnectionStrings:DefaultConnection` from these sources:

- `FoodDiary.Web.Api/appsettings.json`
- `FoodDiary.Web.Api/appsettings.{Environment}.json`
- `FOODDIARY_ConnectionStrings__DefaultConnection`
- `--connection-string "..."`

The last source wins.

Running `update` without a target migration also bootstraps the initial administrator after migrations complete.
Configure it with `InitialAdmin__Email` and `InitialAdmin__Password`. The bootstrap is idempotent and skips
creation when the password is absent or the user already exists. `InitialAdmin__BootstrapTimeoutSeconds`
controls its timeout and defaults to 30 seconds.

## Notes

- EF Core migrations stay in `FoodDiary.Infrastructure` next to `FoodDiaryDbContext`.
- `FoodDiary.Initializer` only executes operational commands.
- `rollback-last` is a convenience wrapper around migrating to the previous applied migration.
