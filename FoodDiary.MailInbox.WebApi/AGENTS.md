# Mail Inbox WebApi Guidelines

## Scope
Rules for `FoodDiary.MailInbox.WebApi/`.

## Role
- Dedicated inbound email service host for FoodDiary.
- Keep this project host-focused: `Program.cs`, configuration, Docker host assets, health endpoints, and runtime wiring.
- Keep endpoints in `FoodDiary.MailInbox.Presentation`.
- Keep use cases in `FoodDiary.MailInbox.Application`.
- Keep PostgreSQL, SMTP listener, and MIME parsing in `FoodDiary.MailInbox.Infrastructure`.
