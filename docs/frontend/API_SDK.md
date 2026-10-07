# Generated API clients

FoodDiary generates an Angular `HttpClient` SDK from the actual host's OpenAPI
document. The user API client covers 41 API groups and 262 operations. Generated transport contracts
live in `FoodDiary.Web.Client/src/app/shared/api/sdk/generated/`; application
models, UI state and response normalization remain in the existing feature
adapters. This keeps auth/refresh/retry interceptors and cookie behavior in the
same HTTP pipeline.

This is an internal generated API layer with application adapters. It is not a
standalone `@fooddiary/sdk` package or a universal `FoodDiaryClient` facade.
Capacitor packages the same Angular application. Existing auth state, session
refresh and retry behavior stay in their current owners.

## Commands

Run from `FoodDiary.Web.Client/`, with Node 22+, Java 21+ and the repository's
.NET SDK installed:

- `npm run sdk:update`: export the current host contract and regenerate the SDK.
- `npm run sdk:generate`: regenerate using the committed user API contract.
- `npm run sdk:check`: check committed generated code without changing files.
- `npm run sdk:check:api`: export the actual API and verify both contract and SDK.
- `npm run test:sdk`: test scope selection, schema closure and drift prerequisites.
- `npm run sdk:admin:update`: export the same host and regenerate the admin client.
- `npm run sdk:admin:generate`: regenerate from `api-sdk/admin.openapi.json`.
- `npm run sdk:admin:check`: verify the committed admin contract and generated code.
- `npm run sdk:admin:check:api`: verify the admin contract against the actual API.
- `npm run sdk:bot:update`: export the host and regenerate the Telegram bot C# client.
- `npm run sdk:bot:generate`: regenerate from the committed bot contract.
- `npm run sdk:bot:check`: check reproducibility without an API export.
- `npm run sdk:bot:check:api`: check the selected bot contract against the actual host.
- `npm run check:api-client-usage`: reject handwritten HTTP requests in the user and admin applications and test the guard's negative fixtures.

The exporter uses `TransportApiWebApplicationFactory`, disables hosted workers
and does not require a database, credentials or external providers. Export and
.NET build outputs go to repository-level `.artifacts/sdk/`. The generator's
version and SHA-256 are pinned in `api-sdk/generator.json`. Its JAR is downloaded
from Maven Central into `.artifacts/sdk/tools/` and verified before execution.
The regeneration pipeline uses TypeScript to remove unused template imports
and Prettier to format output; strict application compiler settings stay enabled.

## Working with the SDK

1. Change the owning presentation contract, update existing API snapshots when
   needed, and run `npm run sdk:update`.
2. Review `api-sdk/user.openapi.json`, `api-sdk/scopes.json` and the generated diff together.
3. Adjust feature adapters and run the relevant frontend tests and build.
4. Commit the contract and generated TypeScript alongside the API change.

Never edit generated files by hand. Import a concrete generated service/model
from a feature API adapter; components and facades continue using the feature's
public application models. The generated SDK describes wire values; UI-specific
enum normalization, dates and error fallbacks belong in adapters. Products and
public products, all three favorites services, hydration, weight/waist history,
goals, weekly goals/check-in, comments, likes, reports, USDA/Open Food Facts,
wearable connections, billing, recommendations/client tasks, gamification,
shopping lists, fasting, meals, recipes/public recipes, exploration, meal plans,
lessons, cycles, statistics, dashboards, the dietologist workspace, user profiles,
profile measurements, recipe lookup and notifications consume generated clients.
Authentication, Telegram authentication, active sessions, AI/recognition, image
upload coordination, file export, telemetry and marketing attribution use them
as well: 46 user frontend services in total. Direct presigned storage uploads retain their
original `SKIP_AUTH` transport, and recognition SignalR/polling orchestration
remains in `FoodRecognitionService`.

The selector preserves server parameter names, response codes, nullability,
validation and auth metadata. It assigns stable client operation names and
includes explicitly selected user groups and transitively referenced schemas.
Nested scopes use the most specific prefix: auth, Telegram auth, sessions and bot
operations have separate generated clients. Added or removed operations require
a review of the frozen scope manifest. New modules can be
added as separate slices instead of generating unrelated Admin APIs into
the user app. The admin client has its own contract and frozen scope manifest:
`api-sdk/admin.openapi.json` and `api-sdk/admin.scopes.json`. Its 21 groups and
72 operations are generated into
`projects/fooddiary-admin/src/app/shared/api/sdk/generated/` and consumed by
21 existing admin services. Both applications reuse the pinned generator and
contract selector while retaining their existing import boundary.

Admin adapters retain their application models, collection paging and legacy
filter omission/whitespace rules. Open filter records use an explicit query
bridge; fixed query values continue through generated request parameters.
Admin HTTP keeps its existing interceptor pipeline and cookie policy: ordinary
requests omit cross-origin credentials, while SSO exchange enables them.
SSO tokens and state remain in `AdminAuthService`. Presigned admin image uploads
continue through the existing `HttpBackend` client. Import/test idempotency keys
are created once per invocation and survive re-subscription.

Admin meal-plan lists use `CatalogPlanSummary`, matching the actual summary
response. Detail/editor flows still fetch `CatalogPlan` with days and meals.
No placeholder days or extra detail requests are introduced into list loading.
Both client contracts are checked in CI and both generated outputs in
`npm run verify`.

`createSdkConnection` delegates to the existing Angular interceptor chain with
cookies enabled. `sdkRequestOptions` preserves per-call headers and HttpContext,
including language selection and loading suppression. The adapter retains the
application's camel-case query names and decoded HttpParams values; ASP.NET query
binding is case-insensitive. Repeated values, zeroes and explicit false survive.
Idempotency keys use generated method parameters when declared by the API.

Response adapters check required application fields, preserve nullable fields,
decode UI enums and retain existing error fallbacks. The exporter enriches its
isolated host's schema with C# nullable-reference metadata and `allOf` extensions,
preserving property nullability without marking additional request fields as
required. The published v1 Swagger document remains compatible; a separate
real-host test verifies its existing snapshots. Empty/204
results still need the Angular runtime null bridge; `sdkOptional` keeps the existing null
behavior for those calls. `sdkMaybe` preserves omitted optional values separately
from explicit null. Only values represented by application Date objects are
decoded into Date; calendar dates, birth dates, cycle boundaries and request
reporting ranges retain their existing encoding. Nutrition values are copied
without recalculation or rounding. Meals and dashboard snapshots share the
existing meal normalization, including saved product/recipe snapshots and AI
nutrition behavior. Recipe serving lookup consumes only its required subset.

Lessons retains the legacy array response for rolling deployments. Its typed
category/difficulty queries use the schema's lowercase values, accepted by the
server's case-insensitive binding. Cycles preserves explicit nullable request
sections and all clear flags. Birth-date updates retain explicit
null for clearing and the existing UTC-midnight wire encoding.

Cross-feature capability bindings in `composition/feature-action.providers.ts`
load their owning implementations only when subscribed to. The lazy adapter
retains one cold request per operation invocation: retries preserve idempotency
keys, cancellation during module loading prevents an API call, and cancellation
after loading still cancels HTTP. Implementations remain Angular DI singletons.
Production build measurements fell from 1,085.02 kB initially to 978.53 kB after
lazy bindings and migration of the remaining APIs, below the unchanged 1 MB
warning budget (estimated transfer: 218.26 kB to 204.73 kB).

CSV/PDF exports use generated Blob responses, preserving attachment filenames
and range/locale parameters. The generation pipeline checks binary response
handling, including media types beginning with `text/`; video import retains
multipart file and field behavior.

## AI development and compatibility

The isolated export enables API Explorer only for the internal Logs and
MarketingAttribution controllers. Their three routes stay hidden in published
Swagger. Telemetry adapters keep their original cookie policy and
`SKIP_AUTH`/`SKIP_OBSERVABILITY` contexts; attribution keeps its first-touch state,
idempotency keys and error suppression. The retired generic `ApiService` has no
remaining production consumers.

The Telegram bot uses a separate selected contract and 14 generated operations
under `FoodDiary.Telegram.Bot/Api/Generated/`. The pinned C# generator uses the
repository's small templates with `HttpClient` and `System.Text.Json`; no new
project or runtime dependency is added. Generated methods send typed request DTOs
and return raw responses owned by the bot adapters. Those adapters retain response
size limits, expected status handling, cancellation, lease/security-version and
receipt checks, and durable checkpoint/retry rules. API credentials stay on each
request. The legacy water call supplies the idempotency header required by its
existing server contract and uses the existing named clients without redirects.
UTC timestamps and calendar dates retain their native `DateTime`/`DateOnly`
representations. The bot's four displayed nutrition totals retain `decimal`
through a client-only generation hint; statistics retain the server's `double`
representation. Raw signed uploads and Telegram downloads remain in their owners.

Bot scope selection freezes exact consumed methods while preserving transitive
schema closure. CI checks the bot's live contract and deterministic C# output.

## Preventing manual API clients from returning

Generation drift checks are complemented by source architecture guards. The
frontend guard uses TypeScript types to reject direct HttpClient method references
(including aliases, bracket calls and destructuring), HttpBackend dispatch, native
fetch/XMLHttpRequest/beacon entrypoints, httpResource and alternate HTTP-library
imports in both application roots. It runs through `lint`, `verify`, CI and
pre-push. Generated output is exempt only at its two exact owned paths.

Exceptions match the concrete file, owning method, HTTP verb and target shape:
generated transport forwarding, existing interceptor forwarding, signed storage
PUTs and static translation JSON GETs. A new API request in an exempt file is
still rejected. SignalR continues using its own client and connection lifecycle.

`GeneratedApiClientUsageTests` in the backend architecture suite applies the same
policy to the Telegram bot using Roslyn symbols, rejecting direct HTTP dispatch,
HTTP request construction and handwritten FoodDiary route literals. Existing
transport forwarding and signed storage upload shapes are the only handwritten
HTTP exceptions. Negative fixtures exercise old-client regressions, renamed
clients, detached aliases and additional requests inside exception files.

New transport exceptions require an explicit policy change and
regression coverage; do not widen them to entire feature areas.

For an AI agent, generated methods and DTOs provide concrete API evidence and
compiler feedback instead of manually duplicated HTTP assumptions. Read the
relevant adapter, generated method and presentation contract; do not load the
whole SDK into context. Add new client operations through generation and run
the checks above before handing off API changes.

CI checks the live API contract as well as reproducibility. This complements
the existing breaking-change API checks and integration snapshots; generated
types do not prove runtime business semantics or compatibility with an already
installed mobile client. Capacitor bundles the Angular SDK with the app. Old
mobile versions still require backward-compatible server contracts.
