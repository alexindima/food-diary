# Mail Relay Initializer Guidelines

## Scope
Rules for `FoodDiary.MailRelay.Initializer/`.

## Role
- Thin console host for MailRelay operational database tasks.
- Initialize and inspect the MailRelay PostgreSQL schema.
- Do not start HTTP, RabbitMQ consumers, SMTP delivery, or background workers here.

## Commands
- Build: `dotnet build FoodDiary.MailRelay.Initializer/FoodDiary.MailRelay.Initializer.csproj`
- Status: `dotnet run --project FoodDiary.MailRelay.Initializer -- status`
- Update schema: `dotnet run --project FoodDiary.MailRelay.Initializer -- update`

