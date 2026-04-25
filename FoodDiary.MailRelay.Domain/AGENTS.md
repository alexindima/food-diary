# Mail Relay Domain Guidelines

## Scope
Rules for `FoodDiary.MailRelay.Domain/`.

## Role
- Own mail relay domain concepts and rules once they are extracted from application/infrastructure flows.
- Keep ASP.NET, PostgreSQL, RabbitMQ, SMTP, DNS, DKIM, and options out of this project.
- Prefer small entities/value objects/policies with behavior over passive duplicates of persistence records.

## Commands
- Build: `dotnet build FoodDiary.MailRelay.Domain/FoodDiary.MailRelay.Domain.csproj`
