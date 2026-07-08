# Mail Inbox Application Guidelines

## Scope
Rules for `MailInbox/FoodDiary.MailInbox.Application/`.

## Role
- Own inbound mail use cases, application models, and abstractions.
- Keep HTTP, PostgreSQL, SMTP listener, MIME parsing implementation, and host configuration out of this project.
- Keep AI task suggestion orchestration here once that workflow is added.
- Depend only on `MailInbox/FoodDiary.MailInbox.Domain`, `Shared/FoodDiary.Results`, and `Shared/FoodDiary.Mediator` among local production projects.

## Structure
- Organize by feature or purpose folders, not a flat `Services/` bucket.
- Keep namespaces aligned with folder paths.

## Rules
- Do not reference MailInbox client, infrastructure, presentation, or host projects.
- Do not reference ASP.NET, EF Core/Npgsql, MailKit/MimeKit, SmtpServer, `IConfiguration`, or `IOptions<T>`.
- Async application interfaces should accept `CancellationToken`.

## Commands
- Build: `dotnet build MailInbox/FoodDiary.MailInbox.Application/FoodDiary.MailInbox.Application.csproj`
- Tests: `dotnet test MailInbox/tests/FoodDiary.MailInbox.Tests/FoodDiary.MailInbox.Tests.csproj`
