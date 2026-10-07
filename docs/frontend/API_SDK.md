# Generated user API SDK

FoodDiary generates an Angular `HttpClient` SDK from the actual host's OpenAPI
document. The internal API client covers 39 API groups and 259 operations. Generated transport contracts
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
upload coordination and file export use them as well: 44 frontend services in
total. Direct presigned storage uploads retain their
original `SKIP_AUTH` transport, and recognition SignalR/polling orchestration
remains in `FoodRecognitionService`.

The selector preserves server parameter names, response codes, nullability,
validation and auth metadata. It assigns stable client operation names and
includes explicitly selected user groups and transitively referenced schemas.
Nested scopes use the most specific prefix: auth, Telegram auth, sessions and bot
operations have separate generated clients. Added or removed operations require
a review of the frozen scope manifest. New modules can be
added as separate slices instead of generating unrelated Admin/auth APIs into
the user app. A future Admin consumer should use a shared workspace library;
the current admin-to-client import boundary still applies.

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
