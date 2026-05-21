# Mail Inbox Initializer Guidelines

## Scope
Rules for `FoodDiary.MailInbox.Initializer/`.

## Role
- Thin console host for MailInbox operational database tasks.
- Apply MailInbox migrations and run explicit initialization/backfill routines.

## Architecture
- Keep persistence mappings and migrations in `FoodDiary.MailInbox.Infrastructure`.
- Reuse MailInbox application and infrastructure services instead of duplicating logic.
- Do not expose HTTP endpoints or SMTP listener behavior from this project.
- Keep commands safe to run from CI/CD and server shells.

## Operational Practices
- Prefer idempotent initialization and backfill flows.
- Keep destructive operations explicit and parameterized.
- Never commit production connection strings or local secrets.

## Commands
- Build: `dotnet build FoodDiary.MailInbox.Initializer/FoodDiary.MailInbox.Initializer.csproj`
