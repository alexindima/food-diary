# Backend Module Map

Use this file when deciding where backend code belongs.

## Primary FoodDiary Backend

| Concern | Project | Put Here | Do Not Put Here |
| --- | --- | --- | --- |
| Domain model | `FoodDiary.Domain` | Entities, value objects, aggregate behavior, domain events | EF Core, HTTP, external SDKs, options |
| Application ports/models | `FoodDiary.Application.Abstractions` | Feature ports, application-facing models, shared result abstractions | ASP.NET, EF Core, provider SDKs, host config |
| Application runtime | `FoodDiary.Application.Runtime` | Mediator pipeline behaviors, transaction boundary, post-commit queue registration | Feature handlers, validators, business services, module aggregation |
| Use cases | Owning `FoodDiary.Application.<Feature>` project or `Modules/<Feature>/Application` | Commands, queries, handlers, validators, application services | Cross-feature shared buckets, persistence implementation, HTTP request/response DTOs |
| Fasting contracts | `Modules/Fasting/Contracts` | Stable cross-module read DTOs/read service and operational job contracts | Repositories, aggregates, handlers, EF, HTTP transport |
| Fasting domain | `Modules/Fasting/Domain` | Fasting aggregates, enums, identifiers, and invariants | Application orchestration, EF mappings, transport |
| Fasting application ports | `Modules/Fasting/Application/Abstractions` | Repository ports and internal persistence projections | Stable cross-module contracts, EF implementations |
| Fasting use cases | `Modules/Fasting/Application` | Fasting commands, queries, handlers, validators, application services and registration | Persistence implementations, HTTP transport, foreign module internals |
| Fasting persistence model | `Modules/Fasting/Infrastructure/Model` | Fasting EF configurations and the model-builder registration seam | Shared `DbContext`, migrations, repository behavior |
| Fasting infrastructure | `Modules/Fasting/Infrastructure` | Fasting repository implementations and full module registration | HTTP transport, jobs, central migrations |
| Billing use cases | `FoodDiary.Application.Billing` | Billing commands, queries, validators, renewal and webhook orchestration | Core Application dependencies, persistence, HTTP DTOs |
| Marketing use cases | `FoodDiary.Application.Marketing` | Attribution commands, queries and conversion recording | Core Application dependencies, persistence, HTTP DTOs |
| Notification use cases | `FoodDiary.Application.Notifications` | Notification feed, preferences, web-push subscription and delivery orchestration | Core Application dependencies, persistence implementations, HTTP DTOs |
| Persistence/technical implementations | `FoodDiary.Infrastructure` | DbContext, EF mappings, repositories, technical service implementations | HTTP controllers, host startup, external provider orchestration |
| External adapters | `FoodDiary.Integrations` | Provider clients, provider options, MailRelay/MailInbox client bridges | EF migrations, core domain workflows |
| HTTP/SignalR transport | `FoodDiary.Presentation.Api` | Controllers, hubs, HTTP requests/responses, presentation mappings | Business logic, infrastructure, host middleware |
| Host/composition | `FoodDiary.Web.Api` | Program, DI wiring, auth, middleware, Swagger, rate limiting, telemetry exporters | Feature controllers, request DTOs, domain rules |
| Resources | `FoodDiary.Resources` | Notification/report text providers and resources | Business orchestration, persistence, host config |
| Operational initialization | `FoodDiary.Initializer` | Migration/status/list orchestration, safe seed/backfill entrypoints | Domain rules, HTTP transport, background scheduler plumbing |
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

MailRelay placement rules:
- Runtime/provider options belong in infrastructure `Options/`, except `MailRelayOptions` in application and `MailRelayClientOptions` in client.
- WebApi is host-only; endpoint controllers and HTTP DTOs belong in presentation.
- Package references are layer-specific and guarded by architecture tests.

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

MailInbox placement rules:
- SMTP/runtime options belong in infrastructure `Options/`, client options in client, and HTTP presentation options in presentation.
- WebApi is host-only; endpoint controllers and HTTP DTOs belong in presentation.
- Package references are layer-specific and guarded by architecture tests.

## Placement Checklist

Before adding a file:
- Is it domain invariant/behavior? Put it in domain.
- Is it a use case or business workflow? Put it in application.
- Is it an interface/model needed by adapters? Put it in application abstractions near the feature.
- Is it EF/provider/worker implementation? Put it in infrastructure or integrations.
- Is it HTTP transport shape or mapping? Put it in presentation.
- Is it startup/DI/middleware/configuration? Put it in the host.
- Is it service-to-service MailRelay/MailInbox access from core FoodDiary? Put it in `FoodDiary.Integrations` and use the client package.
- Is it reusable UI? Put it in the frontend UI kit.
- Is it feature UI? Put it in the frontend feature folder.

If the answer is "shared", first ask whether it is truly cross-feature. Many things belong in a feature-specific `Common/`, `Models/`, `api/`, or `lib/` folder instead of a global bucket.

For backend repository contracts:
- Projection/counter/summary reads belong on `*ReadModelRepository`.
- Existence-only checks belong on a narrow `*LookupRepository`.
- Aggregate mutation paths should use `*WriteRepository`; add aggregate `*ReadRepository` only when the workflow needs domain aggregates.
- Avoid injecting full composite `*Repository` contracts into application code when a narrower contract exists.

Backend structure guardrails now enforce the high-level placement rules. In particular:
- application feature code belongs in feature purpose folders, not new flat folders;
- presentation controllers should depend only on presentation-safe collaborators;
- executable hosts keep only `Program.cs` in the project root;
- JobManager remains a scheduler/worker host and must not own persistence, mediator, or HTTP workflows directly.
