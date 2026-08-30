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
| Hydration contracts | `Modules/Hydration/Contracts` | Stable hydration read service and projection models used by Dashboard and Weekly Check-In | Repositories, handlers, EF, HTTP transport |
| Hydration domain compatibility seam | `FoodDiary.Domain/Entities/Tracking`, `FoodDiary.Domain/ValueObjects/Ids`, `FoodDiary.Domain/Entities/Users` | `HydrationEntry`, its identifier, invariants, and `User.HydrationEntries` | Application orchestration, EF mappings, transport |
| Hydration application ports | `Modules/Hydration/Application/Abstractions` | Hydration repository ports and persistence projections | Stable cross-module contracts and EF implementations |
| Hydration use cases | `Modules/Hydration/Application` | Hydration commands, queries, handlers, validators, services, and registration | Persistence implementations and HTTP transport |
| Hydration persistence model | `Modules/Hydration/Infrastructure/Model` | Hydration EF configuration and model-builder registration seam | Shared `DbContext`, migrations, repository behavior |
| Hydration infrastructure | `Modules/Hydration/Infrastructure` | Hydration repository implementation and complete module registration | HTTP transport and central migrations |
| WeeklyGoals contracts | `Modules/WeeklyGoals/Contracts` | Stable weekly-goal read model and read-service contract | Repositories, handlers, EF, HTTP transport |
| WeeklyGoals domain | `Modules/WeeklyGoals/Domain` | Weekly-goal aggregate, enum, identifier, invariants, and stable CLR namespace/EF identity | Application orchestration, EF mappings, transport, shared `User` ownership |
| WeeklyGoals application ports | `Modules/WeeklyGoals/Application/Abstractions` | Weekly-goal repository and serialized-transaction ports | EF implementations and host concerns |
| WeeklyGoals use cases | `Modules/WeeklyGoals/Application` | Weekly-goal commands, queries, handlers, validation, progress calculation, reminder processing, and application registration | Persistence implementations and HTTP transport |
| WeeklyGoals persistence model | `Modules/WeeklyGoals/Infrastructure/Model` | WeeklyGoals EF configuration and model-builder registration seam | Shared `DbContext`, migrations, repository behavior |
| WeeklyGoals infrastructure | `Modules/WeeklyGoals/Infrastructure` | WeeklyGoals repository, advisory-lock transaction runner, and complete module registration | HTTP transport, scheduler plumbing, and central migrations |
| TDEE use cases | `Modules/Tdee/Application` | TDEE calculation, insight query/model, user-profile composition, validation, and module registration | Domain entities, persistence, provider adapters, HTTP transport, or empty wrapper layers |
| Lessons contracts | `Modules/Lessons/Contracts` | Stable lesson administration capabilities and projection models consumed by Admin | Repository ports, aggregates, EF, HTTP transport |
| Lessons domain | `Modules/Lessons/Domain` | `NutritionLesson`, `UserLessonProgress`, identifiers, enums, and invariants with preserved CLR identity | Application orchestration, EF mapping, transport |
| Lessons application ports | `Modules/Lessons/Application/Abstractions` | Lesson repository ports and internal persistence projections | Stable cross-module contracts and EF implementations |
| Lessons use cases | `Modules/Lessons/Application` | Lesson commands, queries, handlers, validators, services, mappings, and application registration | Persistence implementations and HTTP transport |
| Lessons persistence model | `Modules/Lessons/Infrastructure/Model` | Lesson EF configurations and model-builder registration seam | Shared `DbContext`, migrations, repository behavior |
| Lessons infrastructure | `Modules/Lessons/Infrastructure` | Lesson repository implementation and complete module registration | HTTP transport and central migrations |
| Daily Advices domain | `Modules/DailyAdvices/Domain` | Aggregate, identifier, invariants, stable CLR/EF identity | Application orchestration, EF mappings, transport |
| Daily Advices application ports | `Modules/DailyAdvices/Application/Abstractions` | Repository port and persistence projection | EF implementation or cross-module aggregate exposure |
| Daily Advices use cases | `Modules/DailyAdvices/Application` | Query, model, selection, application registration | Persistence implementation and HTTP transport |
| Daily Advices persistence model | `Modules/DailyAdvices/Infrastructure/Model` | EF configuration and model-builder seam | Shared `DbContext`, migrations, repository behavior |
| Daily Advices infrastructure | `Modules/DailyAdvices/Infrastructure` | Repository and complete module registration | HTTP transport and central migrations |
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
