# FoodDiary.MailRelay.Client Guidelines

## Role
Typed client package for service-to-service calls into MailRelay.

## Rules
- Keep this project independent from MailRelay application, infrastructure, presentation, and host projects.
- Put public HTTP contract DTOs under `Models/`.
- Keep DI wiring under `Extensions/`.
- Keep configuration classes under `Options/`.
- Root files should stay limited to `IMailRelayClient.cs` and `MailRelayClient.cs`.
- Do not add persistence, server-side transport implementation, ASP.NET server types, RabbitMQ, MailKit/MimeKit, DNS, or MediatR dependencies here.
- Keep request/response DTOs stable; changes can affect service-to-service callers.

## Commands
- Build: `dotnet build FoodDiary.MailRelay.Client/FoodDiary.MailRelay.Client.csproj`
