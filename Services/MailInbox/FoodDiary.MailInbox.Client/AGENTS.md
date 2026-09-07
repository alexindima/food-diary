# FoodDiary.MailInbox.Client Guidelines

## Role
Typed client package for service-to-service calls into MailInbox.

## Rules
- Keep this project independent from MailInbox application, infrastructure, presentation, and host projects.
- Put public HTTP contract DTOs under `Models/`.
- Keep DI wiring under `Extensions/`.
- Keep configuration classes under `Options/`.
- The `Export/` folder owns recipient-filtered pagination and binary MIME export, independently of server assemblies.
- Root files should stay limited to `IMailInboxClient.cs` and `MailInboxClient.cs`.
- Do not add persistence, server-side transport implementation, ASP.NET server types, SMTP listener, MailKit/MimeKit, or MediatR dependencies here.
- Keep request/response DTOs stable; changes can affect service-to-service callers.

## Commands
- Build: `dotnet build Services/MailInbox/FoodDiary.MailInbox.Client/FoodDiary.MailInbox.Client.csproj`
