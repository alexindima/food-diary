# Mail Inbox Infrastructure Guidelines

## Scope
Rules for `FoodDiary.MailInbox.Infrastructure/`.

## Role
- Implement mail inbox application abstractions.
- Own PostgreSQL storage, SMTP listener integration, MIME parsing implementation, hosted services, and typed infrastructure options.
- Do not expose HTTP endpoints from this project.
