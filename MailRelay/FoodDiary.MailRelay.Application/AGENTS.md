# Mail Relay Application Guidelines

## Scope
Rules for `MailRelay/FoodDiary.MailRelay.Application/`.

## Role
- Own relay use cases, application models, and abstractions.
- Keep HTTP, PostgreSQL, RabbitMQ, SMTP, DNS, and host configuration out of this project.
- Application services may coordinate abstractions such as queue storage, delivery transport, and dispatch notification.
- Depend only on `MailRelay/FoodDiary.MailRelay.Domain` and `Shared/FoodDiary.Mediator` among local production projects.

## Structure
- Organize by feature or purpose folders, not a flat `Services/` bucket.
- Keep interfaces close to the use case/purpose they support.
- Keep namespaces aligned with folder paths.

## Rules
- Do not reference MailRelay client, infrastructure, presentation, or host projects.
- Do not reference ASP.NET, EF Core/Npgsql, RabbitMQ, MailKit/MimeKit, DNS, `IConfiguration`, or `IOptions<T>`.
- Async application interfaces should accept `CancellationToken`.
- Keep transport/persistence DTOs out of application models unless they are deliberate application contracts.

## Commands
- Build: `dotnet build MailRelay/FoodDiary.MailRelay.Application/FoodDiary.MailRelay.Application.csproj`
- Tests: `dotnet test MailRelay/tests/FoodDiary.MailRelay.Tests/FoodDiary.MailRelay.Tests.csproj`
