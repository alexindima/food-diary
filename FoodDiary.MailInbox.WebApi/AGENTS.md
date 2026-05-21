# Mail Inbox WebApi Guidelines

## Scope
Rules for `FoodDiary.MailInbox.WebApi/`.

## Role
- Dedicated inbound email service host for FoodDiary.
- Keep this project host-focused: `Program.cs`, configuration, Docker host assets, health endpoints, and runtime wiring.
- Keep endpoints in `FoodDiary.MailInbox.Presentation`.
- Keep use cases in `FoodDiary.MailInbox.Application`.
- Keep PostgreSQL, SMTP listener, and MIME parsing in `FoodDiary.MailInbox.Infrastructure`.
- Use MVC controller mapping from `FoodDiary.MailInbox.Presentation`; do not add minimal API `/api/mail-inbox` endpoints in `Program.cs`.
- Runtime configuration must use the separate MailInbox database (`fooddiary_mailinbox`) and its own initializer.

## Commands
- Build: `dotnet build FoodDiary.MailInbox.WebApi/FoodDiary.MailInbox.WebApi.csproj`
- Run: `dotnet run --project FoodDiary.MailInbox.WebApi`
- Tests: `dotnet test tests/FoodDiary.MailInbox.Tests/FoodDiary.MailInbox.Tests.csproj`
