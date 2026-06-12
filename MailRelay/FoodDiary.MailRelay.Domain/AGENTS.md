# Mail Relay Domain Guidelines

## Scope
Rules for `MailRelay/FoodDiary.MailRelay.Domain/`.

## Role
- Own mail relay domain concepts and rules once they are extracted from application/infrastructure flows.
- Keep ASP.NET, PostgreSQL, RabbitMQ, SMTP, DNS, DKIM, and options out of this project.
- Prefer small entities/value objects/policies with behavior over passive duplicates of persistence records.

## Rules
- Do not reference MailRelay application, client, infrastructure, presentation, or host projects.
- Keep framework/provider types out of domain code.
- Keep namespaces aligned with folder paths.

## Commands
- Build: `dotnet build MailRelay/FoodDiary.MailRelay.Domain/FoodDiary.MailRelay.Domain.csproj`
