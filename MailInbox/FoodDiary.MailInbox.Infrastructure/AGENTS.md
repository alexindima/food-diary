# Mail Inbox Infrastructure Guidelines

## Scope
Rules for `MailInbox/FoodDiary.MailInbox.Infrastructure/`.

## Role
- Implement mail inbox application abstractions.
- Own PostgreSQL storage, SMTP listener integration, MIME parsing implementation, hosted services, and typed infrastructure options.
- Do not expose HTTP endpoints from this project.
- Depend on `MailInbox/FoodDiary.MailInbox.Application`; do not reference MailInbox presentation or host projects.

## Rules
- Keep inbound parsing and persistence implementation details here.
- Keep SMTP listener options and hosted services here.
- Keep storage changes idempotent where ingestion can retry.
- Avoid logging message bodies or secrets by default.

## Commands
- Build: `dotnet build MailInbox/FoodDiary.MailInbox.Infrastructure/FoodDiary.MailInbox.Infrastructure.csproj`
