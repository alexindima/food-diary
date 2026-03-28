# Web API Host Guidelines

## Scope
Rules for `FoodDiary.Web.Api/`.

## Role
- Treat this project as the executable host and composition root for the HTTP API.
- Keep ASP.NET transport endpoints, request/response DTOs, presentation mappings, authorization attributes, and hubs in `FoodDiary.Presentation.Api`.
- Keep host concerns here: configuration binding, DI wiring, authentication setup, middleware pipeline, Swagger/OpenAPI setup, rate limiting, output caching, telemetry/exporters, and environment-specific behavior.

## Architecture
- Do not add feature controllers or HTTP transport models directly to this project unless there is a deliberate architectural change.
- Keep startup/composition in extensions where practical.
- Keep host policies and defaults explicit so production behavior is easy to audit.

## Structure
- Program entrypoint: `Program.cs`
- Composition helpers: `Extensions/`
- Host-level options: `Options/`
- Swagger/OpenAPI host configuration: `Swagger/`

## Commands
- Build: `dotnet build FoodDiary.Web.Api/FoodDiary.Web.Api.csproj`
- Run: `dotnet run --project FoodDiary.Web.Api`

## Host Practices
- Keep business logic and HTTP transport behavior out of this project; wire existing modules together instead.
- Keep cross-cutting concerns in middleware, filters, option validation, or host-level pipeline behaviors as appropriate.
- Keep telemetry/exporter wiring in this host project, not in `FoodDiary.Presentation.Api`.
- New host configuration sections should use typed options in `Options/` and validate on startup when practical.
- Do not commit real secrets or local passwords to `appsettings*.json`; keep repository config as safe placeholders only.
- Avoid logging sensitive query values. If auth tokens must travel in query for transport reasons, do not enable global query-string logging without explicit redaction.
- Do not trust raw `X-Forwarded-*` headers directly in rate limiting, auth, or logging decisions; only honor forwarded client metadata after explicit trusted proxy/network configuration.

## Migration Guidance
- If host logic grows, split it by composition concern under `Extensions/`, `Options/`, or `Swagger/` instead of introducing feature transport code here.
- Keep namespaces aligned with folders.
