# Mail Relay Infrastructure Guidelines

## Scope
Rules for `MailRelay/FoodDiary.MailRelay.Infrastructure/`.

## Role
- Implement mail relay application abstractions.
- Own PostgreSQL queue/outbox/inbox storage, RabbitMQ broker integration, SMTP submission, direct-to-MX delivery, DNS lookup, DKIM signing, hosted workers, telemetry export registration, and typed infrastructure options.
- Do not expose HTTP endpoints from this project.
- Depend on `MailRelay/FoodDiary.MailRelay.Application`; do not reference MailRelay presentation or host projects.

## Structure
- Keep typed infrastructure options under `Options/`.
- Keep `MailRelayOptions` in application and `MailRelayClientOptions` in client; other MailRelay runtime options belong here.

## Rules
- Keep queue/outbox/inbox writes transactional where application semantics require it.
- Treat database queue state as source of truth even when RabbitMQ is active.
- Keep hosted worker retry, locking, and batch settings configurable.
- Avoid exposing message bodies or secrets through diagnostics/logs by default.

## Commands
- Build: `dotnet build MailRelay/FoodDiary.MailRelay.Infrastructure/FoodDiary.MailRelay.Infrastructure.csproj`
