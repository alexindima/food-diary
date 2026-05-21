# Mail Inbox Domain Guidelines

## Scope
Rules for `FoodDiary.MailInbox.Domain/`.

## Role
- Own inbound mail domain concepts and rules.
- Keep ASP.NET, PostgreSQL, SMTP listener, MIME parsing, AI, and options out of this project.
- Prefer small value objects/entities with behavior over persistence-shaped records.

## Rules
- Do not reference MailInbox application, client, infrastructure, presentation, or host projects.
- Keep framework/provider types out of domain code.
- Keep namespaces aligned with folder paths.

## Commands
- Build: `dotnet build FoodDiary.MailInbox.Domain/FoodDiary.MailInbox.Domain.csproj`
