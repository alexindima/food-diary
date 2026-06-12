# Mail Inbox WebApi Guidelines

## Scope
Rules for `MailInbox/FoodDiary.MailInbox.WebApi/`.

## Role
- Dedicated inbound email service host for FoodDiary.
- Keep this project host-focused: `Program.cs`, configuration, Docker host assets, health endpoints, and runtime wiring.
- Keep endpoints in `MailInbox/FoodDiary.MailInbox.Presentation`.
- Keep use cases in `MailInbox/FoodDiary.MailInbox.Application`.
- Keep PostgreSQL, SMTP listener, and MIME parsing in `MailInbox/FoodDiary.MailInbox.Infrastructure`.
- Use MVC controller mapping from `MailInbox/FoodDiary.MailInbox.Presentation`; do not add minimal API `/api/mail-inbox` endpoints in `Program.cs`.
- Runtime configuration must use the separate MailInbox database (`fooddiary_mailinbox`) and its own initializer.

## Commands
- Build: `dotnet build MailInbox/FoodDiary.MailInbox.WebApi/FoodDiary.MailInbox.WebApi.csproj`
- Run: `dotnet run --project MailInbox/FoodDiary.MailInbox.WebApi`
- Tests: `dotnet test MailInbox/tests/FoodDiary.MailInbox.Tests/FoodDiary.MailInbox.Tests.csproj`
