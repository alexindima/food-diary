# Generated API SDK pilot

FoodDiary generates an Angular `HttpClient` SDK from the actual host's OpenAPI
document. The first slice covers the Products API. Generated transport contracts
live in `FoodDiary.Web.Client/src/app/shared/api/sdk/generated/`; application
models, UI state and response normalization remain in the existing feature
adapters. This keeps auth/refresh/retry interceptors and cookie behavior in the
same HTTP pipeline.

## Commands

Run from `FoodDiary.Web.Client/`, with Node 22+, Java 21+ and the repository's
.NET SDK installed:

- `npm run sdk:update`: export the current host contract and regenerate the SDK.
- `npm run sdk:generate`: regenerate using the committed Products contract.
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
2. Review `api-sdk/products.openapi.json` and the generated diff together.
3. Adjust feature adapters and run the relevant frontend tests and build.
4. Commit the contract and generated TypeScript alongside the API change.

Never edit generated files by hand. Import a concrete generated service/model
from a feature API adapter; components and facades continue using the feature's
public application models. The generated SDK describes wire values; UI-specific
enum normalization, dates and error fallbacks belong in adapters. The pilot
initially connects product suggestions and deletion; other ProductService
methods retain their existing transport pending migration of their richer UI
models.

The selector preserves server parameter names, response codes, nullability,
validation and auth metadata. It assigns stable client operation names and
includes only Products and transitively referenced schemas. Added or removed
Products operations require an explicit selector review. New modules can be
added as separate slices instead of generating unrelated Admin/auth APIs into
the user app. A future Admin consumer should use a shared workspace library;
the current admin-to-client import boundary still applies.

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
