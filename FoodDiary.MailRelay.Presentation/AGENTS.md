# Mail Relay Presentation Guidelines

## Scope
Rules for `FoodDiary.MailRelay.Presentation/`.

## Role
- Own HTTP controllers, request authorization, provider webhook request models, and provider webhook payload mapping.
- Keep controllers thin: rely on relay API-key filter, map endpoint-specific payloads, call application through MediatR `ISender`, and map endpoint-specific success or error response.
- Do not reference `FoodDiary.MailRelay.Infrastructure`.

## Structure
- Base controllers: `Controllers/`
- Feature controllers, request models, response models, and HTTP mappings: `Features/`
- Reusable HTTP error responses and attributes: `Responses/`
- API-key/security helpers: `Security/`
- Registration and endpoint mapping: `Extensions/`

## Naming
- Use `*HttpRequest` for request bodies.
- Use `*HttpQuery` for grouped query parameters.
- Use `*HttpResponse` for HTTP response models.
- Use `*HttpMappings` / `*HttpResponseMappings` for presentation mappings.

## Commands
- Build: `dotnet build FoodDiary.MailRelay.Presentation/FoodDiary.MailRelay.Presentation.csproj`
