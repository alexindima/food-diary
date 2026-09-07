# Mail Inbox WebApi Guidelines

## Scope
Rules for `Services/MailInbox/FoodDiary.MailInbox.WebApi/`.

## Role
- Dedicated inbound email service host for FoodDiary.
- Keep this project host-focused: `Program.cs`, configuration, Docker host assets, health endpoints, and runtime wiring.
- Keep endpoints in `Services/MailInbox/FoodDiary.MailInbox.Presentation`.
- Keep use cases in `Services/MailInbox/FoodDiary.MailInbox.Application`.
- Keep PostgreSQL, SMTP listener, and MIME parsing in `Services/MailInbox/FoodDiary.MailInbox.Infrastructure`.
- Use MVC controller mapping from `Services/MailInbox/FoodDiary.MailInbox.Presentation`; do not add minimal API `/api/mail-inbox` endpoints in `Program.cs`.
- Runtime configuration must use the separate MailInbox database (`fooddiary_mailinbox`) and its own initializer.

## Commands
- Build: `dotnet build Services/MailInbox/FoodDiary.MailInbox.WebApi/FoodDiary.MailInbox.WebApi.csproj`
- Run: `dotnet run --project Services/MailInbox/FoodDiary.MailInbox.WebApi`
- Tests: `dotnet test Services/MailInbox/tests/FoodDiary.MailInbox.IntegrationTests/FoodDiary.MailInbox.IntegrationTests.csproj`
