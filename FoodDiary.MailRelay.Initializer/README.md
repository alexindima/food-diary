# FoodDiary.MailRelay.Initializer

Thin console host for MailRelay database schema tasks.

MailRelay does not use EF Core migrations today. Its schema is owned by `MailRelayQueueStore.EnsureSchemaAsync`, and this initializer runs that operation before the relay service starts.

## Commands

```bash
dotnet run --project FoodDiary.MailRelay.Initializer -- status
dotnet run --project FoodDiary.MailRelay.Initializer -- update
dotnet run --project FoodDiary.MailRelay.Initializer -- update --connection-string "Host=..."
```

## Configuration

The runner resolves `ConnectionStrings:DefaultConnection` from these sources:

- `FoodDiary.MailRelay.WebApi/appsettings.json`
- `FoodDiary.MailRelay.WebApi/appsettings.{Environment}.json`
- user secrets in `Development`
- standard .NET configuration/environment variables
- `FOODDIARY_ConnectionStrings__DefaultConnection`
- `MAILRELAY_ConnectionStrings__DefaultConnection`
- `--connection-string "..."`

The last source wins.

For local development, store the MailRelay database connection string in this project:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=fooddiary_mailrelay;Username=postgres;Password=..." --project FoodDiary.MailRelay.Initializer
```
