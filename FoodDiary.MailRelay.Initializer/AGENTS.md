# Mail Relay Initializer Guidelines

## Scope
Rules for `FoodDiary.MailRelay.Initializer/`.

## Role
- Thin console host for MailRelay operational database tasks.
- Initialize and inspect the MailRelay PostgreSQL schema.
- Do not start HTTP, RabbitMQ consumers, SMTP delivery, or background workers here.
- Reuse MailRelay application and infrastructure services instead of duplicating persistence logic.

## Operational Practices
- Prefer idempotent initialization and backfill flows.
- Keep destructive operations explicit and parameterized.
- Never commit production connection strings or relay secrets.

## Commands
- Build: `dotnet build FoodDiary.MailRelay.Initializer/FoodDiary.MailRelay.Initializer.csproj`
- Status: `dotnet run --project FoodDiary.MailRelay.Initializer -- status`
- Update schema: `dotnet run --project FoodDiary.MailRelay.Initializer -- update`
