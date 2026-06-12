# Mail Inbox Presentation Guidelines

## Scope
Rules for `MailInbox/FoodDiary.MailInbox.Presentation/`.

## Role
- Own HTTP endpoints, request/response DTOs, and mappings for the MailInbox service.
- Keep PostgreSQL, SMTP listener, and MIME parsing implementation out of this project.
- May reference `MailInbox/FoodDiary.MailInbox.Application`; do not reference infrastructure or host projects.

## Structure
- Base controllers: `Controllers/`
- Feature controllers, request models, response models, and HTTP mappings: `Features/`
- Registration and endpoint mapping: `Extensions/`

## Naming
- Use `*HttpRequest` for request bodies.
- Use `*HttpQuery` for grouped query parameters.
- Use `*HttpResponse` for HTTP response models.
- Use `*HttpMappings` / `*HttpResponseMappings` for presentation mappings.

## Rules
- Controllers belong under `Features/`; only base controllers belong in `Controllers/`.
- Do not reference PostgreSQL, MailKit/MimeKit, SmtpServer, or infrastructure implementation types.

## Commands
- Build: `dotnet build MailInbox/FoodDiary.MailInbox.Presentation/FoodDiary.MailInbox.Presentation.csproj`
