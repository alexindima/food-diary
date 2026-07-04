# Backend Module Map

Use this file when deciding where backend code belongs.

## Primary FoodDiary Backend

| Concern | Project | Put Here | Do Not Put Here |
| --- | --- | --- | --- |
| Domain model | `FoodDiary.Domain` | Entities, value objects, aggregate behavior, domain events | EF Core, HTTP, external SDKs, options |
| Application ports/models | `FoodDiary.Application.Abstractions` | Feature ports, application-facing models, shared result abstractions | ASP.NET, EF Core, provider SDKs, host config |
| Use cases | `FoodDiary.Application` | Commands, queries, handlers, validators, application services | Persistence implementation, HTTP request/response DTOs |
| Persistence/technical implementations | `FoodDiary.Infrastructure` | DbContext, EF mappings, repositories, technical service implementations | HTTP controllers, host startup, external provider orchestration |
| External adapters | `FoodDiary.Integrations` | Provider clients, provider options, MailRelay/MailInbox client bridges | EF migrations, core domain workflows |
| HTTP/SignalR transport | `FoodDiary.Presentation.Api` | Controllers, hubs, HTTP requests/responses, presentation mappings | Business logic, infrastructure, host middleware |
| Host/composition | `FoodDiary.Web.Api` | Program, DI wiring, auth, middleware, Swagger, rate limiting, telemetry exporters | Feature controllers, request DTOs, domain rules |
| Resources | `FoodDiary.Resources` | Notification/report text providers and resources | Business orchestration, persistence, host config |
| Jobs | `FoodDiary.JobManager` | Scheduled job host, Hangfire registration, cleanup jobs, fasting notification scheduling, outbox processing plumbing | HTTP presentation, duplicated business logic |
| Telegram | `FoodDiary.Telegram.Bot` | Telegram transport, parsing, worker loop | Direct dependencies on core backend projects |

## MailRelay

| Project | Responsibility |
| --- | --- |
| `FoodDiary.MailRelay.Domain` | Relay domain concepts and rules. |
| `FoodDiary.MailRelay.Application` | Relay use cases, application models, abstractions. |
| `FoodDiary.MailRelay.Infrastructure` | PostgreSQL queue/outbox/inbox, RabbitMQ, SMTP/direct-to-MX, DNS, DKIM, workers, options. |
| `FoodDiary.MailRelay.Presentation` | HTTP controllers, API-key authorization, request/response/mapping. |
| `FoodDiary.MailRelay.WebApi` | Host, configuration, health checks, runtime wiring. |
| `FoodDiary.MailRelay.Client` | Typed service-to-service client and DTOs. |
| `FoodDiary.MailRelay.Initializer` | Operational database initialization. |

## MailInbox

| Project | Responsibility |
| --- | --- |
| `FoodDiary.MailInbox.Domain` | Inbound mail domain concepts and rules. |
| `FoodDiary.MailInbox.Application` | Inbound mail use cases, application models, abstractions. |
| `FoodDiary.MailInbox.Infrastructure` | PostgreSQL storage, SMTP listener, MIME parsing, hosted services, options. |
| `FoodDiary.MailInbox.Presentation` | HTTP controllers, request/response/mapping. |
| `FoodDiary.MailInbox.WebApi` | Host, configuration, health checks, runtime wiring. |
| `FoodDiary.MailInbox.Client` | Typed service-to-service client and DTOs. |
| `FoodDiary.MailInbox.Initializer` | Operational database initialization. |

## Placement Checklist

Before adding a file:
- Is it domain invariant/behavior? Put it in domain.
- Is it a use case or business workflow? Put it in application.
- Is it an interface/model needed by adapters? Put it in application abstractions near the feature.
- Is it EF/provider/worker implementation? Put it in infrastructure or integrations.
- Is it HTTP transport shape or mapping? Put it in presentation.
- Is it startup/DI/middleware/configuration? Put it in the host.
- Is it reusable UI? Put it in the frontend UI kit.
- Is it feature UI? Put it in the frontend feature folder.

If the answer is "shared", first ask whether it is truly cross-feature. Many things belong in a feature-specific `Common/`, `Models/`, `api/`, or `lib/` folder instead of a global bucket.
