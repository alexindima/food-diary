# Backend Module Map

Use this file when deciding where backend code belongs.

## Primary FoodDiary Backend

| Concern | Project | Put Here | Do Not Put Here |
| --- | --- | --- | --- |
| Food quality scoring | `Modules/Products/Domain` | FoodQualityScore and FoodQualityGrade; common formula consumed across modules | Contract-only projects, replacement shared scoring assemblies |
| Product identity and units | `Modules/Products/Domain.Contracts` | ProductId, ProductType, MeasurementUnit | Aggregate dependencies and calculations |
| Nutrient health scoring | `Modules/Usda/Domain` | HealthAreaScore, HealthAreaGrade, HealthAreaScores | Reverse Products dependencies |
| Domain model | Owning `Modules/<Feature>/Domain` | Entities, value objects, aggregate behavior, domain events | EF Core, HTTP, external SDKs, central shared domain buckets |
| Application ports/models | `FoodDiary.Application.Abstractions` | Feature ports, application-facing models, shared result abstractions | ASP.NET, EF Core, provider SDKs, host config |
| Application runtime | `FoodDiary.Application.Runtime` | Mediator pipeline behaviors, transaction boundary, post-commit queue registration | Feature handlers, validators, business services, module aggregation |
| Use cases | Owning `FoodDiary.Application.<Feature>` project or `Modules/<Feature>/Application` | Commands, queries, handlers, validators, application services | Cross-feature shared buckets, persistence implementation, HTTP request/response DTOs |
| BodyMetrics domain | `Modules/BodyMetrics/Domain` | Weight/waist measurement entries, IDs, and invariants with legacy CLR namespaces and a one-way dependency on Users-owned `User`/`UserId`; User-owned goal lifecycle belongs to Users Domain | Reverse User measurement navigations, goal lifecycle, application orchestration, EF mappings, transport |
| BodyMetrics application ports | `Modules/BodyMetrics/Application/Abstractions` | Weight/waist repository ports, read capabilities, errors, and projection models | EF, HTTP transport, provider adapters |
| BodyMetrics use cases | `Modules/BodyMetrics/Application` | Weight/waist commands, queries, handlers, validation, read services, and legacy application assembly identity | Persistence implementations and HTTP transport |
| BodyMetrics persistence model | `Modules/BodyMetrics/Infrastructure/Model` | Entry EF configurations and explicit model-builder registration | Central DbContext, migrations, snapshot, and User-owned goal mappings |
| BodyMetrics infrastructure | `Modules/BodyMetrics/Infrastructure` | Entry repositories and complete module registration | HTTP transport, shared database lifecycle, provider adapters |
| Fasting contracts | `Modules/Fasting/Contracts` | Stable cross-module read DTOs/read service and operational job contracts | Repositories, aggregates, handlers, EF, HTTP transport |
| Fasting domain | `Modules/Fasting/Domain` | Fasting aggregates, enums, identifiers, and invariants | Application orchestration, EF mappings, transport |
| Fasting application ports | `Modules/Fasting/Application/Abstractions` | Repository ports and internal persistence projections | Stable cross-module contracts, EF implementations |
| Fasting use cases | `Modules/Fasting/Application` | Fasting commands, queries, handlers, validators, application services and registration | Persistence implementations, HTTP transport, foreign module internals |
| Fasting persistence model | `Modules/Fasting/Infrastructure/Model` | Fasting EF configurations and the model-builder registration seam | Shared `DbContext`, migrations, repository behavior |
| Fasting infrastructure | `Modules/Fasting/Infrastructure` | Fasting repository implementations and full module registration | HTTP transport, jobs, central migrations |
| Hydration contracts | `Modules/Hydration/Contracts` | Stable hydration read service and projection models used by Dashboard and Weekly Check-In | Repositories, handlers, EF, HTTP transport |
| Hydration domain | `Modules/Hydration/Domain` | `HydrationEntry`, its identifier and invariants with legacy CLR namespaces and a one-way dependency on Users-owned `User`/`UserId` | Reverse User navigation, application orchestration, EF mappings, transport |
| Hydration application ports | `Modules/Hydration/Application/Abstractions` | Hydration repository ports and persistence projections | Stable cross-module contracts and EF implementations |
| Hydration use cases | `Modules/Hydration/Application` | Hydration commands, queries, handlers, validators, services, and registration | Persistence implementations and HTTP transport |
| Hydration persistence model | `Modules/Hydration/Infrastructure/Model` | Hydration EF configuration and model-builder registration seam | Shared `DbContext`, migrations, repository behavior |
| Hydration infrastructure | `Modules/Hydration/Infrastructure` | Hydration repository implementation and complete module registration | HTTP transport and central migrations |
| Favorites domain | `Modules/Favorites/Domain` | Favorite meal, product, and recipe aggregates and identifiers with preserved CLR namespaces | Application orchestration, EF mappings, transport |
| Favorites use cases | `Modules/Favorites/Application` | Favorite commands, queries, validators, mappings, read services, and application registration with preserved assembly identity | Persistence implementations and HTTP transport |
| Favorites persistence model | `Modules/Favorites/Infrastructure/Model` | Favorites EF configurations and model-builder registration seam | Shared `DbContext`, migrations, repository behavior |
| Favorites infrastructure | `Modules/Favorites/Infrastructure` | Favorites repositories and complete module registration | HTTP transport and central migrations |
| WeeklyGoals contracts | `Modules/WeeklyGoals/Contracts` | Stable weekly-goal read model and read-service contract | Repositories, handlers, EF, HTTP transport |
| WeeklyGoals domain | `Modules/WeeklyGoals/Domain` | Weekly-goal aggregate, enum, identifier, invariants, and stable CLR namespace/EF identity | Application orchestration, EF mappings, transport, shared `User` ownership |
| WeeklyGoals application ports | `Modules/WeeklyGoals/Application/Abstractions` | Weekly-goal repository and serialized-transaction ports | EF implementations and host concerns |
| WeeklyGoals use cases | `Modules/WeeklyGoals/Application` | Weekly-goal commands, queries, handlers, validation, progress calculation, reminder processing, and application registration | Persistence implementations and HTTP transport |
| WeeklyGoals persistence model | `Modules/WeeklyGoals/Infrastructure/Model` | WeeklyGoals EF configuration and model-builder registration seam | Shared `DbContext`, migrations, repository behavior |
| WeeklyGoals infrastructure | `Modules/WeeklyGoals/Infrastructure` | WeeklyGoals repository, advisory-lock transaction runner, and complete module registration | HTTP transport, scheduler plumbing, and central migrations |
| Wearables domain | `Modules/Wearables/Domain` | Provider connections, sync entries, enums, IDs, protected-token value object, and stable CLR/EF identity | OAuth transport, EF mappings, provider HTTP calls |
| Wearables application ports | `Modules/Wearables/Application/Abstractions` | Provider client, repository, OAuth state, token protection, transaction, and read-model ports | Provider SDK DTOs, EF, HTTP transport, host configuration |
| Wearables use cases | `Modules/Wearables/Application` | Connection, disconnect, explicit sync, auth URL, daily summary, validation, and legacy application assembly identity | Provider adapters, persistence, HTTP transport |
| Wearables persistence model | `Modules/Wearables/Infrastructure/Model` | Wearables EF configurations and model-builder registration seam | Shared `DbContext`, migrations, repository behavior |
| Wearables infrastructure | `Modules/Wearables/Infrastructure` | Repositories, token/OAuth protection, transaction runner, and complete module registration | Provider HTTP adapters, HTTP presentation, central migrations |
| WeeklyCheckIn use cases | `Modules/WeeklyCheckIn/Application` | Weekly check-in query, summaries, trends, suggestions, user-profile composition, and module registration | Domain entities, persistence, adapters, HTTP transport, or empty wrapper layers |
| TDEE use cases | `Modules/Tdee/Application` | TDEE calculation, insight query/model, user-profile composition, validation, and module registration | Domain entities, persistence, provider adapters, HTTP transport, or empty wrapper layers |
| Export application contracts | `Modules/Export/Application/Abstractions` | Export-specific input limits, diary projection, PDF generator, and report-text provider contracts consumed by adapters and presentation | Export handlers, PDF rendering, HTTP transport, provider networking |
| Export use cases | `Modules/Export/Application` | Diary/cycle export queries, access validation, bounded CSV/file generation, and legacy application assembly identity | Owned domain/persistence, retained files, HTTP transport, PDF/network implementations, background jobs |
| Export PDF adapter | `Modules/Export/Infrastructure` | PDF rendering, chart/image composition and bounded SSRF-protected image HTTP client; hosts explicitly call AddExportInfrastructure | Shared DbContext/migrations, Resources localization, HTTP controllers, retained files, background jobs |
| Statistics use cases | `Modules/Statistics/Application` | Nutrition statistics queries, summary composition, response models, date normalization, and legacy application assembly identity | Domain entities, persistence, provider adapters, HTTP transport, contracts, or empty wrapper layers |
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
| Content Reports module | `Modules/ContentReports` | Creation, moderation contracts, aggregate, persistence model/adapter, and module tests | Central `DbContext`, migrations, HTTP transport, Admin orchestration |
| OpenFoodFacts module | `Modules/OpenFoodFacts` | Public catalog queries, cached-search contract and lifecycle, provider/cache ports, durable cache entity, persistence adapter/model, and focused tests | Provider HTTP transport in Integrations; central `DbContext`, migrations, snapshot, and HTTP presentation |
| USDA module | `Modules/Usda` | USDA reference-data entities, catalog use cases, ports/contracts, EF mappings, repository adapter, and focused tests | Provider HTTP/cache in Integrations; central Product navigation, `DbContext`, migrations, snapshot, and HTTP presentation |
| Images ID contracts | `Modules/Images/Contracts` | Dependency-free `ImageAssetId` with stable CLR namespace | Image aggregate behavior, EF, storage providers |
| Images domain | `Modules/Images/Domain` | `ImageAsset` lifecycle and stable CLR namespace | EF, storage providers, HTTP transport |
| Images application ports | `Modules/Images/Application/Abstractions` | Image access, storage, cleanup, repository and deletion-outbox ports | Provider SDKs, EF implementations, HTTP transport |
| Images use cases | `Modules/Images/Application` | Presign, confirm, delete, resolution, cleanup and legacy application assembly identity | Storage providers, EF, HTTP transport |
| Images persistence model | `Modules/Images/Infrastructure/Model` | `ImageAsset` EF configuration and model-builder seam | Shared DbContext, migrations, outbox engine |
| Images infrastructure | `Modules/Images/Infrastructure` | Image repository/outbox adapters and persistence registration | S3/provider details, HTTP transport, migrations |
| Dietologist domain | `Modules/Dietologist/Domain` | Invitations, permissions, recommendations, client tasks, identifiers, enums, and events | Application orchestration, EF, transport |
| Dietologist application ports | `Modules/Dietologist/Application/Abstractions` | Repository ports, persistence projections, attention and dashboard-access capabilities | EF implementations and HTTP transport |
| Dietologist use cases | `Modules/Dietologist/Application` | Commands, queries, policies, models, services, and application registration | Persistence implementations and HTTP transport |
| Dietologist persistence model | `Modules/Dietologist/Infrastructure/Model` | EF configurations and model-builder seam | Shared `DbContext`, migrations, repository behavior |
| Dietologist infrastructure | `Modules/Dietologist/Infrastructure` | Repositories, attention projection, and complete module registration | HTTP transport and central migrations |
| Billing domain | `Modules/Billing/Domain` | Subscriptions, payments, webhook inbox events, provider names and payment kinds with preserved CLR/EF identity | Application orchestration, EF mappings, provider SDKs, transport |
| Billing application ports | `Modules/Billing/Application/Abstractions` | Repository, checkout lock, transaction runner, provider gateway and provider-facing models | Billing's Marketing conversion port, EF/provider implementations, HTTP transport |
| Billing use cases | `Modules/Billing/Application` | Billing commands, queries, validators, entitlement, renewal and webhook orchestration with preserved `FoodDiary.Application.Billing` assembly identity | Persistence, provider adapters, HTTP DTOs |
| Billing persistence model | `Modules/Billing/Infrastructure/Model` | Billing EF configurations and explicit model-builder registration | Shared `DbContext`, migrations, repository behavior |
| Billing infrastructure | `Modules/Billing/Infrastructure` | Billing repositories, advisory checkout lock, transaction runner and complete module registration | Provider HTTP adapters, HTTP transport, central migrations |
| Marketing application ports | `Modules/Marketing/Application/Abstractions` | Attribution repository ports and persistence projections | Billing's consumer-owned conversion port, EF implementations, HTTP transport |
| Marketing domain | `Modules/Marketing/Domain` | Attribution event, identifier, normalization invariants, and stable CLR/EF identity | Application orchestration, EF mappings, transport |
| Marketing use cases | `Modules/Marketing/Application` | Attribution commands, queries, conversion recording, cleanup and legacy application assembly identity | Persistence implementations, scheduler plumbing, HTTP DTOs |
| Marketing persistence model | `Modules/Marketing/Infrastructure/Model` | Attribution EF configuration and central model-builder seam | Shared `DbContext`, migrations, repository behavior |
| Marketing infrastructure | `Modules/Marketing/Infrastructure` | Attribution repository adapter and complete module registration | HTTP transport, central migrations, cleanup scheduling |
| Notifications | `Modules/Notifications` | Feed/preferences orchestration, notification/subscription aggregates and IDs, application ports/payloads, explicit EF model, repositories/outbox adapter and web-push provider; legacy Application assembly and CLR namespaces preserved | Users-owned preference storage, central DbContext/migrations/snapshot and multi-stream outbox engine/replay, HTTP/SignalR and JobManager composition roots |
| Users | `Modules/Users` | Complete User Domain, UserId contracts, application, role/goal mappings and adapters, focused tests | Shared application contracts, combined UserRepository, DbContext/migrations/snapshot and Identity authentication flows/providers |
| Identity | `Modules/Identity` | Authentication and Email application slices/services; EmailTemplate, UserRefreshTokenSession and UserLoginEvent Domain types, their EF model, template/session/login-event adapters, cached template provider and focused tests; legacy application assembly and CLR namespaces preserved | Shared Authentication/Email abstractions, Users-owned User/security state, combined UserRepository, central DbContext/migrations/snapshot, external provider adapters, HTTP transport and hosts |
| Persistence/technical implementations | `FoodDiary.Infrastructure` | Shared DbContext/migrations/snapshot, shared technical mappings, mixed repositories and technical service implementations; module mappings are registered explicitly | HTTP controllers, host startup, external provider orchestration |
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

## MealPlanning physical ownership

`Modules/MealPlanning` groups two distinct aggregate areas, MealPlans and
ShoppingLists. Application retains its legacy assembly and CLR namespaces;
Application/Abstractions contains both areas' ports, read models and errors.
Domain contains the MealPlan and ShoppingList aggregate graphs, their IDs,
ShoppingList domain events and source enum with stable CLR namespaces.
Infrastructure contains both repositories and complete module DI; its Model
project contains all six EF mappings, explicitly applied by the central context.

The ShoppingList-to-User relationship is one-way: ShoppingList retains scalar
`UserId` and its `User` navigation, while Users-owned User has no inverse ShoppingLists
CLR collection. Its EF mapping uses schema-equivalent `WithMany()` with the same
foreign key and cascade behavior. The module Domain references the existing Users,
Products, Recipes and Meals owners plus shared Primitives through its declared
project references. Source provenance IDs are not foreign keys. Shared DbContext,
historical migrations/snapshot and Presentation controllers remain central;
User cleanup orchestration belongs to Modules/Users/Infrastructure. No empty Contracts layer is created:
IShoppingListCreationService is the existing internal aggregate boundary.

Module tests live under `Modules/MealPlanning/tests`; central projects retain mixed
domain, repository, HTTP and host integration tests.

Exercises ownership: `Modules/Exercises/Application` owns slices and read-service implementation; `Application/Abstractions` owns repository ports/projections/errors; `Contracts` owns IExerciseEntryReadService and ExerciseEntryModel; `Domain` owns ExerciseEntry/ExerciseEntryId/ExerciseType; `Infrastructure/Model` owns explicit EF mapping; `Infrastructure` owns repository and complete DI. Focused Application/Domain tests live under `Modules/Exercises/tests`. Shared DbContext/migrations and HTTP/host/cross-module tests remain central.

## RecipeCommunity logical module

`Modules/RecipeCommunity` owns Application (RecipeComments/RecipeLikes), Application/Abstractions, Domain, Infrastructure and Infrastructure/Model. Legacy application assembly and CLR namespaces remain stable. RecipeCommunity entities and IDs remain separate from Users and Recipes Domain owners through one-way User/Recipe navigations. Shared context/migrations, HTTP and ContentReports reportability projection remain with their owners; no extra Contracts or provider layer. See `docs/ai/recipecommunity-ownership-inventory.md` for sources and compatibility seams.

## Ai physical ownership

Ai owns Application, Application/Abstractions, Domain, Infrastructure/Model and Infrastructure under `Modules/Ai`. Application keeps its legacy assembly/CLR identity. AiUsage/AiPromptTemplate and quota ledger ownership, provider/cache/consent semantics, Users-owned User and DbContext seams and consumers are source-audited in `docs/ai/ai-ownership-inventory.md`. Admin invokes semantic administration capabilities; Meals AI entities remain Meals-owned. Provider HTTP/options remain Integrations. Focused tests live under Modules/Ai/tests; central PostgreSQL/HTTP/mixed suites remain with their owners.

## Dashboard logical extraction

`Modules/Dashboard` owns Application, Application/Abstractions, Contracts and
Infrastructure. It is a read composer with no Domain or PersistenceModel. Stable
statistics contracts retain their CLR namespaces and are referenced one-way by
central Abstractions for Statistics/Cycles/WeeklyCheckIn/Tdee/Gamification consumers.
Optimized projection readers own no contributing aggregates. Shared DbContext,
migrations/model snapshot and HTTP transport remain central. Hosts explicitly call
`AddDashboardReadServices` after infrastructure registration; Application fallback
registration remains `AddDashboardModule`. Scoped concrete/interface aliases and
query behavior are preserved. Owned application/adapter tests live under module
tests; mixed DI/date, shared PostgreSQL and HTTP suites remain central.

## Recipes physical ownership

Recipes use cases, ports, read contracts, persistence model and adapters live under `Modules/Recipes`. Recipe/Steps/Ingredients and value objects/events live in Recipes Domain; recipe IDs live in Recipes Domain.Contracts. Removed inverse CLR navigations remain absent. Shared context/migrations/snapshot and cross-module tests stay central. Hosts compose AddRecipesModule; JobManager uses AddRecipesPersistence without adding application handlers. See `docs/ai/recipes-ownership-inventory.md`; this is not full Domain/database isolation.

## Admin physical ownership

Role-audit reads and both scoped aliases are owned by Admin Infrastructure;
Users still owns the underlying role-audit entities and mappings. See
`docs/ai/admin-role-audit-persistence.md` for the bounded follow-up.

Admin owns application slices, billing-report/impersonation/mail-reader ports,
AdminImpersonationSession Domain, its explicit EF model and reporting/session
adapters under Modules/Admin. Legacy application assembly and CLR namespaces
remain stable; compatibility requires coordinated host rebuilds. Email templates
remain Identity-owned and role audit/User capabilities remain Users-owned despite
legacy Admin namespaces. Shared context/migrations, SSO store/JWT providers,
HTTP authorization, structured audit and MailInbox client bridge remain central.
Hosts call AddAdminModule; JobManager adds only AddAdminPersistence. See
docs/ai/admin-ownership-inventory.md for current source evidence and test ownership.

## Products physical ownership

Products owns Product, ProductId contracts, product-only value objects, use cases,
ports, persistence adapters, EF model and focused tests under `Modules/Products`.
User/Product and Product/MealItem are unidirectional EF relationships; shared
context/migrations and composition lock remain central. Hosts explicitly
compose AddProductsModule; JobManager adds AddProductsPersistence without new
handlers. See `docs/ai/products-ownership-inventory.md` for the boundary and
coordinated-rebuild compatibility promise.

## Meals physical ownership

Meals owns Meal, MealItem, MealAiSession, MealAiItem, their IDs, meal-only states,
nutrition event and AI item/session enums under `Modules/Meals/Domain`, with stable
CLR namespaces. User has no inverse Meals collection; Meal.User remains a one-way
relationship with the same required FK and cascade. Shared User/UserId, enums,
DbContext, migrations and snapshot remain with their existing owners. Product,
Recipe and Image links remain ID-based with unchanged batch snapshot fallbacks.
No extra Domain.Contracts project is needed by the current acyclic graph.
See `docs/ai/meals-ownership-inventory.md` for source evidence and remaining seams.

## RecentItems physical ownership

RecentItems aggregate/ID/enum, narrow usage abstractions, repository, post-commit recorder and EF model live under `Modules/RecentItems`; focused tests are nested there. Central User/UserId, DbContext, migrations/snapshot, post-commit queue/UoW and Users cleanup orchestration remain compatibility seams. Products, Recipes and Meals consume narrow module abstractions, and hosts compose `AddRecentItemsModule`. See `docs/ai/recent-items-ownership-inventory.md`.

## Users Domain ownership

Users owns the complete User aggregate, all credential/security partials, roles,
role audit and weight/waist goals under `Modules/Users/Domain`. `UserId` lives in
`Modules/Users/Domain.Contracts`, depending only on shared primitives. Consumers
reference the exact owner; shared guards and generic values belong to
`FoodDiary.Domain.Primitives`; module-specific values stay with their owner. Authentication flows/providers, combined UserRepository,
DbContext, migrations and snapshot retain their existing owners. CLR namespaces,
security behavior and EF/HTTP contracts are unchanged. See
`docs/ai/users-domain-extraction.md` for residual seams and verification evidence.
