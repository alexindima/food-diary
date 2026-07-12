# Backend patterns and architecture decisions

This document is an evidence-based map of the backend architecture. It is intended both as project documentation and as a preparation guide for a senior backend interview. File references are representative entry points, not an exhaustive list.

## 1. System-level architecture

### 1. Modular monolith

The primary FoodDiary backend is deployed as one application but split into projects and feature modules with explicit dependency rules. This preserves simple deployment and in-process transactions while preventing an unstructured “big ball of mud”. The important senior-level point is that a modular monolith is a deliberate trade-off: microservices are deferred until independent scaling, ownership, availability or release cadence justify their distributed-systems cost.

Evidence: `docs/ARCHITECTURE.md`, `docs/adr/0001-modular-monolith-with-supporting-services.md`, `tests/FoodDiary.ArchitectureTests/ProjectDependencyMatrixTests.cs`.

### 2. Selective service extraction

MailRelay and MailInbox are separate deployable bounded contexts with their own databases, hosts and client packages. They were extracted because SMTP, queues, retries and inbound listeners have a genuinely different operational lifecycle, not merely because they form separate folders. JobManager and Telegram Bot are also separate runtime adapters.

Evidence: `docs/adr/0002-mailrelay-mailinbox-as-separate-services.md`, `MailRelay/`, `MailInbox/`, `FoodDiary.JobManager/`, `FoodDiary.Telegram.Bot/`.

### 3. Bounded contexts and explicit service contracts

MailRelay and MailInbox repeat their own Domain/Application/Infrastructure/Presentation layering and can be accessed from the core only through typed client packages. This limits model leakage and forms an anti-corruption boundary between contexts.

Evidence: `FoodDiary.MailRelay.Client`, `FoodDiary.MailInbox.Client`, `tests/FoodDiary.ArchitectureTests/ClientPackageBoundaryTests.cs`.

### 4. Database per supporting service

The primary product, MailRelay and MailInbox own separate PostgreSQL stores. This creates clear data ownership and independent operational lifecycles, at the cost of losing cross-service ACID transactions and requiring eventual consistency at service boundaries.

Evidence: `docs/ARCHITECTURE.md`, Docker Compose configuration, service infrastructure projects.

## 2. Layering and dependency management

### 5. Clean Architecture

Dependencies point inward: Domain contains business rules; Application contains use cases; Application.Abstractions defines ports; Infrastructure and Integrations implement ports; Presentation adapts HTTP; Web.Api composes the process. Business logic does not depend on ASP.NET Core, EF Core or external SDKs.

Evidence: project references, `docs/BACKEND_MODULE_MAP.md`, `tests/FoodDiary.ArchitectureTests/LayeringTests.cs`.

### 6. Hexagonal Architecture / Ports and Adapters

Interfaces in `FoodDiary.Application.Abstractions` are inbound/outbound ports. EF repositories, email, object storage, AI, wearable and food-provider integrations are adapters. Replacing a provider therefore affects the adapter and DI wiring rather than the use case.

Evidence: `FoodDiary.Application.Abstractions/`, `FoodDiary.Infrastructure/`, `FoodDiary.Integrations/`, ADR 0004.

### 7. Dependency Inversion Principle

High-level policy owns the abstractions needed by low-level implementations. Infrastructure references application contracts, rather than Application referencing Infrastructure. This is the mechanism behind the Clean Architecture dependency direction.

Evidence: `FoodDiary.Application.Abstractions/Common/Abstractions/`, project reference matrix.

### 8. Composition Root

Executable hosts own DI, configuration, authentication, middleware, telemetry and provider wiring, but not feature controllers or business rules. Centralizing object-graph assembly keeps construction concerns out of domain and application code.

Evidence: `FoodDiary.Web.Api/Program.cs`, `FoodDiary.Web.Api/Extensions/`, `tests/FoodDiary.ArchitectureTests/HostCompositionBoundaryTests.cs`.

### 9. Dependency injection and modular registration

Technical areas expose focused registration modules instead of building dependencies ad hoc. This keeps provider options, lifetimes and implementations replaceable and makes composition testable.

Evidence: `FoodDiary.Infrastructure/DependencyInjection.*.cs`, `FoodDiary.Integrations/DependencyInjection.*.cs`.

### 10. Architectural fitness functions

Architecture is enforced by executable tests: dependency matrix, layer rules, host-only boundaries, async conventions, feature structure, package allowlists and side-effect restrictions. This turns architectural intent into a CI constraint and prevents gradual erosion.

Evidence: `tests/FoodDiary.ArchitectureTests/`.

### 11. Architecture Decision Records

Important choices record context, decision, benefits and consequences. ADRs preserve the “why”, which code alone cannot explain, and make later reversals deliberate.

Evidence: `docs/adr/`.

## 3. Application architecture

### 12. Vertical Slice / feature-first organization

Commands, queries, handlers, validators, models and services are grouped by business feature rather than only by technical type. A change is therefore localized around a use case, while common folders are restricted to genuinely cross-feature concepts.

Evidence: `FoodDiary.Application/`, `FoodDiary.Application.Abstractions/`, `tests/FoodDiary.ArchitectureTests/FeatureStructureTests.cs`.

### 13. CQRS

Writes are expressed as commands and reads as queries. Command paths load and mutate aggregates; read paths prefer projections and dedicated read-model services. This is pragmatic CQRS inside one application and database, not two independently deployed systems.

Evidence: `FoodDiary.Application/Common/Abstractions/Messaging/`, command/query handlers, ADR `2026-07-05-product-recipe-read-model-query-paths.md`.

### 14. Mediator pattern

Controllers and other adapters send requests through a local mediator instead of invoking handlers directly. The mediator decouples callers from handler implementations and provides one pipeline for cross-cutting policies.

Evidence: `Shared/FoodDiary.Mediator/DefaultMediator.cs`, request handlers throughout Application.

### 15. Chain of Responsibility / pipeline behaviors

Validation, structured logging and command transaction behavior wrap handlers as an ordered pipeline. Cross-cutting logic is implemented once without contaminating every use case.

Evidence: `FoodDiary.Application/Common/Behaviors/ValidationBehavior.cs`, `LoggingBehavior.cs`, `CommandTransactionBehavior.cs`.

### 16. Result pattern and railway-oriented error flow

Expected failures are returned as `Result`/`Result<T>` with stable error codes and kinds, rather than represented by exceptions. Presentation maps those failures to HTTP responses; exceptions remain for unexpected faults. This produces explicit handler contracts and consistent error semantics.

Evidence: `Shared/FoodDiary.Results/`, `FoodDiary.Presentation.Api/Extensions/ResultExtensions.cs`, `FoodDiary.Web.Api/Extensions/ApiExceptionHandler.cs`.

### 17. FluentValidation at the application boundary

Request validators run before handlers through a mediator behavior, aggregate all failures and return a typed validation result. This separates input/use-case validation from domain invariants, which must still be protected by the domain model.

Evidence: `ValidationBehavior.cs`, feature `*Validator.cs` files.

### 18. Unit of Work

Commands share an `IUnitOfWork`; the command pipeline commits pending changes only after a successful result. This creates one explicit transaction boundary per command and avoids scattered `SaveChanges` calls.

Evidence: `FoodDiary.Application.Abstractions/.../IUnitOfWork.cs`, `FoodDiary.Infrastructure/Persistence/EfUnitOfWork.cs`, `CommandTransactionBehavior.cs`.

### 19. Repository pattern with Interface Segregation

Persistence contracts are split into `ReadRepository`, `WriteRepository`, `LookupRepository` and `ReadModelRepository`. Handlers depend on the narrowest capability they need, reducing accidental aggregate loading and preventing broad “god repositories”.

Evidence: repository contracts in Application.Abstractions, `docs/ARCHITECTURE.md`, `ApplicationGuardrailTests.cs`.

### 20. Read Model / Projection pattern

List, summary, counter and dashboard queries project directly to application-facing read models instead of reconstructing full aggregates. This lowers query cost and decouples UI-shaped reads from the write model.

Evidence: product/recipe read-model ADR, `FoodDiary.Infrastructure/Persistence/Dashboard/`, `*ReadService` and `*ReadModelRepository` implementations.

### 21. Policy objects / application services

Reusable decisions such as current-user access, scheduling and cleanup live behind focused services instead of being duplicated across handlers or controllers. This keeps orchestration explicit without moving domain invariants out of aggregates.

Evidence: `CurrentUserAccessPolicy`, `FastingNotificationScheduler`, cleanup services.

## 4. Domain-Driven Design

### 22. Rich domain model

Entities expose behavior that validates and changes their own state rather than serving as public property bags. Invariants are tested independently of infrastructure.

Evidence: `FoodDiary.Domain/Entities/`, `tests/FoodDiary.Domain.Tests/Domain/*InvariantTests.cs`.

### 23. Aggregates and aggregate roots

Aggregate roots define consistency boundaries and collect domain events. Application commands mutate these boundaries through repositories rather than editing arbitrary persistence rows.

Evidence: `Shared/FoodDiary.Domain.Primitives/AggregateRoot.cs`, aggregate entities such as `Meal`, `Recipe`, `ShoppingList` and `User`.

### 24. Value Objects

Domain concepts and identity values use dedicated immutable types rather than primitives. This makes invalid combinations harder to represent and prevents accidentally mixing unrelated `Guid` values.

Evidence: `FoodDiary.Domain/ValueObjects/`, `FoodDiary.Domain/ValueObjects/Ids/`, value-object invariant tests.

### 25. Strongly typed IDs

`UserId`, `MealId`, `RecipeId` and many other IDs wrap `Guid`. EF converters bridge them to database columns. The trade-off is extra mapping/serialization code in exchange for compile-time type safety.

Evidence: `FoodDiary.Domain/ValueObjects/Ids/`, `StronglyTypedIdConverters` and their tests.

### 26. Domain Events

Aggregates raise facts such as user deletion, invitation acceptance and nutrition changes. An EF `SaveChangesInterceptor` publishes them inside the current transaction, allowing handlers to add transactional state without coupling aggregates to subscribers.

Evidence: entity calls to `RaiseDomainEvent`, `DomainEventDispatchInterceptor.cs`, `docs/backend/BACKEND_EVENT_TAXONOMY.md`.

### 27. Domain Event versus Integration Event taxonomy

The design explicitly distinguishes in-transaction domain facts from committed facts intended for another process. This prevents the common error of treating an in-memory domain event as reliable cross-service messaging.

Evidence: `docs/backend/BACKEND_EVENT_TAXONOMY.md`, `IIntegrationEvent`.

## 5. Consistency, concurrency and reliable side effects

### 28. Transactional Outbox

Email, web push and object deletion are written as database records in the same transaction as business state. JobManager sends them after commit, closing the “database committed but external call was lost” failure window.

Evidence: email, notification and image outbox implementations; ADR `2026-07-05-backend-side-effect-transaction-semantics.md`.

### 29. Competing Consumers with database leases

Outbox processors claim batches using `FOR UPDATE SKIP LOCKED`, a worker identity and expiring leases. Multiple workers can process in parallel without taking the same record, while abandoned work becomes claimable again after a crash.

Evidence: `FoodDiary.Infrastructure/Persistence/Outbox/OutboxMessageClaimer.cs`.

### 30. Retry with backoff and Dead Letter Queue semantics

Outbox records carry attempt, next-attempt and dead-letter state. Transient failures are retried, while permanently failing messages stop poisoning the active queue and remain inspectable.

Evidence: outbox message classes, processors, migrations `AddOutboxMessageLeases` and `AddOutboxDeadLettering`.

### 31. Idempotent Consumer / idempotent HTTP commands

Selected POST endpoints accept an `Idempotency-Key`. The store reserves work, hashes request identity, rejects conflicting reuse/in-progress duplicates and replays a completed response. Redis provides cross-instance coordination; an in-memory implementation supports local/test scenarios.

Evidence: `FoodDiary.Presentation.Api/Filters/IdempotencyFilter.cs`, `FoodDiary.Web.Api/Services/RedisIdempotencyStore.cs`.

### 32. Optimistic concurrency control

Mutable aggregates use EF concurrency tokens. Concurrent writers are detected at commit instead of serialized pessimistically, which is appropriate when conflicts are relatively uncommon.

Evidence: entity mappings and `FoodDiaryDbContextModelSnapshot.cs` occurrences of `IsConcurrencyToken()`.

### 33. Post-commit best-effort actions

`IPostCommitActionQueue` runs non-critical in-memory actions only after a successful commit. The design explicitly reserves it for hints such as live UI refreshes; durable business effects must use an outbox. This distinction is more important than the mechanism itself.

Evidence: `CommandTransactionBehavior.cs`, side-effect transaction ADR.

### 34. Explicit exceptional transaction runners

Workflows such as billing that require their own atomic boundary use a focused transaction runner instead of bypassing the normal unit-of-work policy throughout the codebase. Exceptions to a rule are therefore narrow and reviewable.

Evidence: `FoodDiary.Infrastructure/Persistence/Billing/EfBillingTransactionRunner.cs`, `PersistenceTransactionGuardrailTests.cs`.

## 6. Integration and resilience patterns

### 35. Adapter / Gateway pattern

External systems are hidden behind application-facing interfaces: AI, S3-compatible storage, billing, wearable providers, USDA/Open Food Facts, email and web push. Provider DTOs and SDK exceptions do not become domain concepts.

Evidence: `FoodDiary.Integrations/Services/`, `FoodDiary.Integrations/Wearables/`, provider interfaces in Application.Abstractions.

### 36. Typed service clients

Communication with MailRelay and MailInbox goes through versionable client packages rather than shared internal assemblies or ad hoc HTTP calls. This creates a small explicit contract and preserves bounded-context independence.

Evidence: `FoodDiary.MailRelay.Client`, `FoodDiary.MailInbox.Client`.

### 37. Cache-aside and stale-cache fallback

External food searches cache successful data and may serve stale data during provider degradation. Cache-aside keeps the source of truth external, while stale fallback trades freshness for availability and is exposed in telemetry.

Evidence: `FoodDiary.Application/OpenFoodFacts/Services/OpenFoodFactsCachedProductSearch.cs`, observability baseline.

### 38. Provider fallback / graceful degradation

AI and catalog integrations distinguish transport, timeout, parsing and exhausted-retry outcomes; AI can fall back from a primary model. This avoids turning every dependency degradation into a total product outage.

Evidence: AI provider implementation and tests, `docs/backend/BACKEND_OBSERVABILITY_BASELINE.md`.

### 39. Background Job / Scheduler pattern

Hangfire-backed recurring jobs handle cleanup, notification scheduling and outbox processing outside request latency. The host owns scheduling while application/infrastructure services own the actual behavior.

Evidence: `FoodDiary.JobManager/`, `JobManagerGuardrailTests.cs`, Hangfire registration tests.

## 7. API and presentation architecture

### 40. Presentation adapter and DTO mapping

Controllers, binding models, HTTP responses and mappings live in Presentation.Api. Application requests are transport-neutral and do not know about route, claims, status codes or OpenAPI.

Evidence: `FoodDiary.Presentation.Api/Features/`, ADR 0003.

### 41. Exception Handler / Problem translation

Expected errors flow through Result mapping; unexpected exceptions are handled centrally with a trace identifier and stable error response. Central handling avoids leaking stack traces and keeps controllers focused on transport orchestration.

Evidence: `FoodDiary.Web.Api/Extensions/ApiExceptionHandler.cs`, presentation `ResultExtensions.cs`.

### 42. API contract snapshot testing

Routes, payloads, status codes and OpenAPI output are stored as reviewed snapshots. This makes accidental contract drift visible in a pull request and treats HTTP shape as a compatibility surface.

Evidence: `tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/`, ADR 0005.

### 43. Rate limiting and defense in depth

The host applies rate limiting alongside authentication, authorization, CORS, forwarded-header handling and security headers. These are independent controls: compromising or misconfiguring one does not remove every protection.

Evidence: `FoodDiary.Web.Api/Extensions/`, related Web.Api tests.

### 44. User-scoped output caching

Selected safe read endpoints use explicit output-cache policies with correct user/admin variation. Scoping is critical: caching authenticated data without identity-aware keys can become a data-leak vulnerability.

Evidence: output-cache policies and metrics in Web.Api, observability baseline.

## 8. Observability and operability

### 45. OpenTelemetry and correlation

The host emits traces and metrics at HTTP, database and provider boundaries. Trace identifiers are included in API errors, enabling a client-visible failure to be correlated with server telemetry.

Evidence: OpenTelemetry configuration, request observability middleware, API error responses.

### 46. Structured logging

Logs use stable message templates and named properties rather than string concatenation. The mediator logging behavior records request name, duration and classified error information while avoiding expected validation failures as exception noise.

Evidence: `LoggingBehavior.cs`, `StructuredAuditLogger`.

### 47. Business and dependency metrics

Metrics cover critical auth flows, jobs, AI, external providers, storage, email, database failures and cache effectiveness. This goes beyond generic CPU/request metrics and makes product-impacting degradation diagnosable.

Evidence: `docs/backend/BACKEND_OBSERVABILITY_BASELINE.md`.

### 48. Health checks

The host checks dependencies such as PostgreSQL, distributed cache and S3-compatible storage separately. Health endpoints support orchestration and diagnosis, but should not replace business-level telemetry.

Evidence: Web.Api health-check implementations and tests.

### 49. Audit logging

Security- and business-relevant actions use structured audit records/logging separate from ordinary diagnostic messages. Audit data answers “who did what and when”, whereas application logs primarily explain runtime behavior.

Evidence: `StructuredAuditLogger`, user audit domain types and tests.

## 9. Testing and delivery decisions

### 50. Test pyramid with architectural tests

The repository separates fast domain/application tests, infrastructure unit tests, PostgreSQL/Testcontainers integration tests, presentation tests, host integration tests and contract snapshots. Each suite protects a different failure class instead of relying only on expensive end-to-end tests.

Evidence: `docs/TESTING_STRATEGY.md`, `tests/`, service-specific test projects.

### 51. Testcontainers for real persistence behavior

PostgreSQL integration tests verify mappings, migrations, transactions, repository semantics and query plans against the actual database engine. This catches behavior that EF in-memory substitutes cannot reproduce.

Evidence: `tests/FoodDiary.Infrastructure.IntegrationTests/` and service integration tests.

### 52. Migration safety as a governed practice

Migrations are treated as operational changes: generated files are committed together, formatting is checked and migration safety is integration-tested. This acknowledges that schema deployment risk is different from ordinary application-code risk.

Evidence: `docs/backend/BACKEND_MIGRATION_SAFETY.md`, migration integration tests, repository guidelines.

## 10. Deeper security and identity architecture

### 53. Short-lived access tokens plus stateful refresh sessions

Access is represented by JWTs, while refresh capability is backed by persisted sessions. This hybrid design keeps ordinary authorization stateless but allows administrators and users to inspect and revoke long-lived sessions. It deliberately avoids the weakness of fully stateless refresh tokens, which cannot be reliably revoked before expiry.

Evidence: `FoodDiary.Infrastructure/Authentication/JwtTokenGenerator.cs`, `UserRefreshTokenSession`, refresh-session repositories and auth session endpoints.

### 54. Refresh-token rotation with replay grace window

Every successful refresh rotates the stored token hash. The immediately previous hash remains valid for a short grace period, accommodating concurrent browser requests without permanently accepting an old token. The senior-level trade-off is security versus distributed-client race tolerance: no grace window can produce false logout; a long window increases replay exposure.

Evidence: `FoodDiary.Application/Authentication/Services/AuthenticationTokenService.cs`, `RefreshTokenCommandHandler.cs`, `UserRefreshTokenSession.Rotate`.

### 55. Secret hashing at rest

Refresh tokens are stored as one-way hashes rather than plaintext bearer credentials. A database leak therefore does not directly yield reusable refresh tokens. Verification uses the same canonical hashing scheme and constant-time equality.

Evidence: `FoodDiary.Application/Authentication/Common/SecurityTokenGenerator.cs`, authentication token service.

### 56. Constant-time secret comparison

API keys, token hashes and webhook signatures use `CryptographicOperations.FixedTimeEquals` instead of ordinary string equality where appropriate. This reduces timing side channels when comparing secrets.

Evidence: `SecurityTokenGenerator`, `WearableOAuthStateService`, MailRelay `ProviderWebhookAuthorizer`, presentation `SecretComparison`.

### 57. Signed OAuth state / tamper-evident continuation data

Wearable OAuth state is signed with HMAC-SHA256 and expiry-checked. The callback can trust that user/session continuation data was issued by this backend and was not modified by the browser, mitigating state tampering and OAuth CSRF-style attacks.

Evidence: `FoodDiary.Infrastructure/Authentication/WearableOAuthStateService.cs`.

### 58. Password hashing abstraction

Password verification is isolated behind an application-facing port and implemented in infrastructure. This prevents a hashing library or work-factor choice from leaking into use cases and makes algorithm upgrades testable.

Evidence: password hasher contract, `FoodDiary.Infrastructure/Services/PasswordHasher.cs`, password hasher tests.

### 59. Role-based and policy-based authorization

Declarative roles protect broad capabilities while focused policies/guards cover contextual decisions such as current-user access and impersonation. Authentication (“who are you?”) is kept separate from authorization (“may you perform this operation?”).

Evidence: Presentation authorization/policy folders, current-user access services, admin and impersonation flows.

### 60. Controlled administrator impersonation

Impersonation is modeled as an explicit privileged flow with access guards and audit/login-event context rather than silently rewriting identity. This supports support operations while making a high-risk capability visible and governable.

Evidence: `StartAdminImpersonationCommandHandler.cs`, `ImpersonationAccessGuardMiddleware.cs`, related tests.

### 61. Signed webhook verification and replay protection

Mailgun callbacks are authenticated using provider signatures and timestamp freshness. AWS SNS callbacks validate canonical signed content and certificates. Authenticating the transport is essential because webhook payload fields themselves are attacker-controlled.

Evidence: MailRelay `ProviderWebhookAuthorizer` and `MailRelayPresentationTests`.

### 62. SSRF-resistant remote certificate retrieval

Before downloading an AWS SNS signing certificate, the authorizer validates the certificate URL/host and later checks the certificate chain and RSA signature. This prevents an attacker-provided webhook from turning the backend into a generic internal-network HTTP client.

Evidence: MailRelay `ProviderWebhookAuthorizer.IsTrustedSnsCertificateHost` and its tests.

### 63. Trusted-proxy boundary

Forwarded client IP/scheme headers are honored only through explicit forwarded-header configuration. Rate limiting, logging or security decisions must not trust arbitrary `X-Forwarded-*` values sent by an internet client.

Evidence: Web.Api forwarded-header configuration/middleware and tests, `FoodDiary.Web.Api/AGENTS.md`.

### 64. Fail-closed service API-key configuration

Mail service authorization rejects requests when protection is required but configuration is absent or invalid; disabling a requirement is an explicit operational choice. Fail-closed behavior is safer than silently exposing administrative service endpoints after misconfiguration.

Evidence: MailRelay and MailInbox API-key authorization filters/options and presentation tests.

## 11. Deeper persistence and query architecture

### 65. Persistence ignorance with Fluent API mapping

Domain entities do not carry EF-specific mapping behavior. Infrastructure supplies `IEntityTypeConfiguration<T>` classes for columns, relationships, conversions, indexes and concurrency. The domain remains testable without EF while persistence remains explicit.

Evidence: `FoodDiary.Infrastructure/Persistence/Configurations/`, Domain project boundaries.

### 66. Database-enforced invariants

Unique and composite indexes enforce rules such as one like/favorite/link per key combination. Domain checks give useful errors, while database constraints remain the final defense against concurrent writers bypassing a check-then-insert race.

Evidence: configurations for recipe likes, favorites, lesson progress, meal-plan days, fertility signals and other link entities.

### 67. Query-object style filtering

Read paths pass explicit filter records/objects to read services instead of growing methods with many loosely related parameters. This makes filters composable and stabilizes repository/read-service contracts as search features evolve.

Evidence: `ProductQueryFilters`, `RecipeQueryFilters`, admin billing filters.

### 68. No-tracking projection reads

Read-only repositories use `AsNoTracking` and direct projection where aggregate mutation is unnecessary. This reduces change-tracker memory/CPU and signals that returned data is not a write model.

Evidence: read repositories and services throughout `FoodDiary.Infrastructure/Persistence/`.

### 69. Bulk database operations for cleanup

Cleanup paths use database-side deletion/update mechanisms where aggregate behavior is not required. This avoids materializing large retention batches and is suitable for explicitly technical lifecycle operations.

Evidence: cleanup services/repositories and their PostgreSQL integration tests.

### 70. Time abstraction for deterministic workflows

Application workflows with expiry, deletion, impersonation or rotation logic depend on `TimeProvider` instead of reading the system clock directly. Tests can freeze/advance time without sleeps, and time becomes an explicit dependency.

Evidence: authentication, admin and user command handlers accepting `TimeProvider`; application guidelines.

### 71. UTC normalization policy

Domain timestamps are normalized to UTC, while date-only concepts receive an explicit UTC-date treatment. Export/display offsets are applied at the boundary. This reduces ambiguity caused by server locale and daylight-saving transitions.

Evidence: Domain guidelines, domain factory/mutation methods, export handlers, `docs/backend/BACKEND_TIME_POLICY.md`.

### 72. Soft deletion and restoration lifecycle

Users have explicit delete/restore behavior and corresponding domain events rather than being immediately physically removed. This supports recovery, retention and downstream cleanup, but requires every authorization/read boundary to respect inactive/deleted state.

Evidence: `FoodDiary.Domain/Entities/Users/User.Lifecycle.cs`, current-user access policy, user cleanup jobs.

### 73. Retention pipeline: logical delete then physical cleanup

Account deletion marks business state first; scheduled cleanup later removes eligible data and external objects. Separating user-facing deletion from heavy/destructive cleanup shortens request latency and provides a controlled retention window.

Evidence: user/image cleanup services, JobManager cleanup jobs, backend runbooks.

## 12. Deeper messaging and mail architecture

### 74. Database queue as source of truth with broker acceleration

MailRelay treats PostgreSQL queue state as authoritative even when RabbitMQ is enabled. The broker can reduce polling latency, but losing a broker notification does not lose the email because durable state remains queryable. This is a useful hybrid alternative to treating the broker itself as the only durable record.

Evidence: MailRelay infrastructure guidelines, queue store and RabbitMQ notification implementations.

### 75. Inbox / deduplication pattern

MailRelay and MailInbox persist identifiers/state so retried provider, SMTP or broker deliveries can be recognized rather than applied twice. This complements at-least-once transport and is the receiving-side counterpart of the outbox pattern.

Evidence: MailRelay inbox/queue storage, MailInbox idempotent ingestion storage, integration tests.

### 76. Store-and-forward mail delivery

Mail requests are accepted into durable queue state before SMTP delivery. Client request latency and availability are decoupled from recipient MX/server availability; delivery can retry independently and expose status.

Evidence: MailRelay queue application flow, PostgreSQL queue store and delivery workers.

### 77. Strategy pattern for mail transports/providers

SMTP relay, direct-to-MX and provider-related paths are selected behind application abstractions/configuration. The orchestration does not need to know DNS, SMTP session or provider SDK details.

Evidence: MailRelay delivery transports, DNS/MX services and application transport contracts.

### 78. DKIM signing as an outbound adapter concern

DKIM signing is performed in MailRelay infrastructure, where raw MIME and cryptographic delivery details belong. Neither the core FoodDiary domain nor MailRelay application rules depend on the signing library.

Evidence: MailRelay DKIM infrastructure services/options.

### 79. SMTP envelope as authoritative routing metadata

MailInbox treats SMTP envelope recipients as delivery truth and MIME `To:` only as fallback metadata. Headers can be absent, rewritten or unrelated to the actual recipient, so transport-level routing information has higher authority.

Evidence: `SmtpInboundMessageStore`, MailInbox infrastructure guidelines and tests.

### 80. MIME/DMARC parsing adapter

Raw mail and aggregate-report formats are parsed in infrastructure into application/domain-facing models. Complex and potentially hostile MIME/XML provider input is kept outside the domain model.

Evidence: MailInbox MIME and `DmarcReportParser` implementations/tests.

### 81. Liveness versus readiness separation

Service liveness answers whether the process is running; readiness verifies required database schema objects and dependencies needed to serve traffic. A mere successful TCP/database connection is not sufficient after a partial migration.

Evidence: MailInbox/MailRelay health queries and schema-aware readiness checks.

## 13. Additional delivery and governance decisions

### 82. Options pattern with startup validation

Provider and host configuration is bound to focused typed option classes and validated during startup where practical. Invalid security, URL, batch or timeout configuration fails early instead of surfacing on the first production request.

Evidence: `Options/` folders, DI modules and options-validation tests across hosts and services.

### 83. Cancellation propagation

Async contracts accept `CancellationToken`, and presentation passes `HttpContext.RequestAborted` through mediator and repository/provider calls. This releases work when clients disconnect and supports graceful shutdown for workers.

Evidence: base controllers, async architecture guardrail tests, application abstractions guidelines.

### 84. Async naming as an architectural convention

Backend async methods use the `Async` suffix except documented framework entry points. Enforcing this mechanically improves call-site readability and makes accidentally blocking alternatives easier to identify.

Evidence: `tests/FoodDiary.ArchitectureTests/AsyncMethodGuardrailTests.cs`.

### 85. Localization provider abstraction

Backend-generated notification, report and email text is supplied by a separate Resources adapter that depends only on application-facing contracts. Business workflows request semantic text without owning `.resx` lookup or locale fallback details.

Evidence: `FoodDiary.Resources/`, resource provider contracts and resource guardrail tests.

### 86. Thin operational initializer

Database status, migration, seed and backfill operations run through an explicit console composition root. Keeping them outside API startup avoids hidden deployment-time mutations and makes destructive operational commands intentional.

Evidence: `FoodDiary.Initializer/Program.cs`, Initializer guidelines and guardrail tests.

### 87. Anti-corruption adapter for Telegram

The Telegram worker maps bot commands/callbacks to public client-facing API contracts and cannot reference core backend projects. It remains a transport adapter instead of becoming a second implementation of business rules.

Evidence: `FoodDiary.Telegram.Bot/`, Telegram architecture guidelines and tests.

### 88. Privacy-aware diagnostic design

Mail bodies, secrets and sensitive query values are excluded from logging by default. Telemetry uses bounded classifications and identifiers rather than raw sensitive payloads. Observability is designed with data exposure risk in mind, not added indiscriminately.

Evidence: host and mail infrastructure guidelines, structured telemetry implementations.

## What is deliberately not claimed

The codebase uses several mechanisms that resemble broader patterns, but this guide avoids overstating them:

- There is CQRS, but not full event sourcing: current state is stored directly and domain events are not the authoritative history.
- There are separate bounded contexts, but the primary backend is not a complete microservice architecture.
- Database outboxes provide durable at-least-once work, but there is no global exactly-once guarantee.
- Read projections are CQRS read models, but not necessarily independently deployed materialized views.
- Provider fallback and retry behavior are resilience techniques; they should not automatically be called a circuit breaker unless an explicit open/half-open/closed state machine is present.
- Feature folders improve cohesion, but they do not by themselves prove that every feature is a true DDD bounded context.

## How to present this architecture in a senior interview

A strong answer should not be a list of pattern names. Use the following structure:

1. **Context:** “The core product needs rapid iteration and strong consistency; email workloads have different scaling and reliability characteristics.”
2. **Decision:** “We chose a modular monolith with Clean Architecture, and extracted only MailRelay/MailInbox as operationally independent bounded contexts.”
3. **Mechanism:** “Architecture tests enforce references; commands go through validation/logging/transaction behaviors; durable external effects use outboxes.”
4. **Failure model:** “A crash after commit does not lose email because the outbox is transactional; leases expire after worker failure; poison messages are dead-lettered.”
5. **Trade-off:** “We accept more interfaces, mappings and projects in exchange for replaceable adapters and enforceable boundaries. We avoid microservice network and consistency cost until there is real pressure.”
6. **Evolution path:** “Read-heavy paths can get dedicated projections; modules can be extracted behind existing ports/client contracts; observability tells us where scaling or reliability pressure actually exists.”

Useful concise summary:

> FoodDiary is a feature-first modular monolith built with Clean/Hexagonal Architecture and tactical DDD. Application use cases use CQRS over a mediator pipeline, explicit Results, validation and a unit-of-work transaction boundary. Domain events run transactionally, while durable external side effects use leased, retryable outboxes processed by a separate job host. Operationally distinct mail capabilities are separate bounded contexts behind typed clients. Architecture tests, contract snapshots, real PostgreSQL integration tests and OpenTelemetry keep these decisions enforceable and observable.

## Important nuances to mention

- CQRS here does **not** imply separate databases or eventual consistency for every read; it means distinct command and projection-oriented query models.
- Domain events are dispatched before commit, so their handlers may add transactional state but must not perform irreversible external I/O.
- The outbox guarantees durable at-least-once processing, not magical exactly-once delivery; consumers and providers still need idempotency.
- Repository abstractions do not automatically improve architecture; their narrow read/write/lookup split is what limits coupling.
- A modular monolith is not “a step before real architecture”; it is often the lowest-cost architecture that meets the current consistency and delivery requirements.
- Architecture tests protect structural rules, but they cannot prove good domain boundaries or prevent all semantic coupling; code review and ADRs remain necessary.
