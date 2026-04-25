# Mail Relay Application Guidelines

## Scope
Rules for `FoodDiary.MailRelay.Application/`.

## Role
- Own relay use cases, application models, and abstractions.
- Keep HTTP, PostgreSQL, RabbitMQ, SMTP, DNS, and host configuration out of this project.
- Application services may coordinate abstractions such as queue storage, delivery transport, and dispatch notification.

## Commands
- Build: `dotnet build FoodDiary.MailRelay.Application/FoodDiary.MailRelay.Application.csproj`
