# Mail Relay Infrastructure Guidelines

## Scope
Rules for `FoodDiary.MailRelay.Infrastructure/`.

## Role
- Implement mail relay application abstractions.
- Own PostgreSQL queue/outbox/inbox storage, RabbitMQ broker integration, SMTP submission, direct-to-MX delivery, DNS lookup, DKIM signing, hosted workers, telemetry export registration, and typed infrastructure options.
- Do not expose HTTP endpoints from this project.

## Commands
- Build: `dotnet build FoodDiary.MailRelay.Infrastructure/FoodDiary.MailRelay.Infrastructure.csproj`
