# Presentation Extraction Plan

## Goal

Split the current `FoodDiary.Web.Api` responsibilities into:

- a thin host / composition root project
- a separate ASP.NET presentation adapter project

Target intent:

- `FoodDiary.Web.Api` remains the executable host
- controllers, API-specific base classes, result mapping, HTTP helpers, and related MVC code move out of the host

This matches the existing guideline that `FoodDiary.Web.Api` should be presentation + composition root today, while moving it toward composition-root-first structure.

## Why This Refactor Makes Sense

Right now `FoodDiary.Web.Api` contains at least four kinds of concerns:

- host startup and composition: [`Program.cs`](./Program.cs), [`Extensions/ApiServiceCollectionExtensions.cs`](./Extensions/ApiServiceCollectionExtensions.cs), [`Extensions/ApiApplicationBuilderExtensions.cs`](./Extensions/ApiApplicationBuilderExtensions.cs)
- MVC presentation surface: feature controllers under [`Features/`](./Features)
- HTTP adapter helpers: [`Controllers/BaseApiController.cs`](./Controllers/BaseApiController.cs), [`Controllers/AuthorizedController.cs`](./Controllers/AuthorizedController.cs), [`Extensions/ResultExtensions.cs`](./Extensions/ResultExtensions.cs), [`Extensions/UserExtensions.cs`](./Extensions/UserExtensions.cs)
- SignalR web adapter pieces: [`Hubs/EmailVerificationHub.cs`](./Hubs/EmailVerificationHub.cs), [`Services/EmailVerificationNotifier.cs`](./Services/EmailVerificationNotifier.cs), [`Services/UserIdProvider.cs`](./Services/UserIdProvider.cs)

That works, but the project is doing both of these jobs:

- executable host
- ASP.NET adapter

Separating them gives cleaner boundaries, clearer testability, and less accidental coupling between hosting concerns and HTTP surface concerns.

## Recommended Target Structure

Recommended project layout:

- `FoodDiary.Web.Api`
  - executable host
  - `Program.cs`
  - host-only composition extensions
  - environment-specific config
  - Swagger setup
  - auth wiring
  - middleware pipeline wiring
  - endpoint registration calls
- `FoodDiary.Presentation.Api`
  - controllers
  - controller base classes
  - MVC/HTTP result mapping
  - claims helpers used only by HTTP surface
  - SignalR hubs and SignalR-specific web adapter services

Recommended dependency direction:

- `FoodDiary.Web.Api` -> `FoodDiary.Presentation.Api`
- `FoodDiary.Web.Api` -> `FoodDiary.Application`
- `FoodDiary.Web.Api` -> `FoodDiary.Infrastructure`
- `FoodDiary.Presentation.Api` -> `FoodDiary.Application`
- `FoodDiary.Presentation.Api` -> `FoodDiary.Contracts`
- `FoodDiary.Presentation.Api` -> `FoodDiary.Domain` only if required for HTTP adapter types such as `UserId`

Important:

- `FoodDiary.Infrastructure` must not reference the new presentation project
- `FoodDiary.Application` must not reference the new presentation project
- `FoodDiary.Domain` must not reference the new presentation project

## Naming Recommendation

Best practical option:

- `FoodDiary.Presentation.Api`

Why:

- It makes the adapter role explicit.
- It avoids overloading `Web.Api` with both host and presentation semantics.
- It leaves room for future adapters such as admin host, gRPC, minimal API, or separate public/private hosts.

Less preferred alternatives:

- `FoodDiary.Web.Presentation`
- `FoodDiary.Api.Presentation`

I would avoid naming the new project just `Presentation`, because that is too vague in a multi-project solution.

## What Should Move

### Move to `FoodDiary.Presentation.Api`

Controllers:

- everything under [`Features/`](./Features)

Controller infrastructure:

- [`Controllers/BaseApiController.cs`](./Controllers/BaseApiController.cs)
- [`Controllers/AuthorizedController.cs`](./Controllers/AuthorizedController.cs)

HTTP / MVC helpers:

- [`Extensions/ResultExtensions.cs`](./Extensions/ResultExtensions.cs)
- [`Extensions/UserExtensions.cs`](./Extensions/UserExtensions.cs)

SignalR adapter pieces:

- [`Hubs/EmailVerificationHub.cs`](./Hubs/EmailVerificationHub.cs)
- [`Services/EmailVerificationNotifier.cs`](./Services/EmailVerificationNotifier.cs)
- [`Services/UserIdProvider.cs`](./Services/UserIdProvider.cs)

Potentially also:

- presentation-specific registration methods such as `AddPresentationApi()`
- presentation-specific endpoint mapping such as `MapPresentationApi()`

### Keep in `FoodDiary.Web.Api`

Executable host:

- [`Program.cs`](./Program.cs)
- [`appsettings.json`](./appsettings.json)
- [`appsettings.Development.json`](./appsettings.Development.json)
- [`Properties/launchSettings.json`](./Properties/launchSettings.json)

Host / composition root concerns:

- environment setup
- configuration binding
- authentication and authorization wiring
- CORS policy setup
- Swagger registration
- calling `AddApplication()`
- calling `AddInfrastructure(...)`
- calling the new presentation registration and mapping methods

## Boundary Recommendation for DI and Pipeline

The cleanest split is:

- host owns cross-cutting composition
- presentation owns MVC/SignalR registration

Recommended API:

- `FoodDiary.Presentation.Api.DependencyInjection.AddPresentationApi(this IServiceCollection services)`
- `FoodDiary.Presentation.Api.DependencyInjection.MapPresentationApi(this IEndpointRouteBuilder app)` or `UsePresentationApi(this WebApplication app)`

### Host should own

- `AddApplication()`
- `AddInfrastructure(configuration)`
- JWT config
- CORS config
- Swagger config
- `AddAuthentication()`
- `AddAuthorization()`

### Presentation project should own

- `AddControllers()`
- controller assembly registration
- SignalR registration if hubs live in presentation
- presentation-only services such as `UserIdProvider` and `EmailVerificationNotifier`
- controller/hub endpoint mapping

This keeps the host in control of runtime composition, while the adapter project exposes only the services and endpoints it needs.

## Alternative Split to Avoid

Do not move only controllers and leave all controller infrastructure in the host.

That would create awkward dependencies like:

- presentation controllers referencing base classes from host
- presentation project depending on `FoodDiary.Web.Api`

That would invert the intended relationship and make the extraction mostly cosmetic.

If the move happens, move the full HTTP adapter slice together:

- controllers
- controller base types
- result mapping
- user/claims helpers
- hubs and adapter services tied to HTTP/SignalR

## Recommended Project Type for `FoodDiary.Presentation.Api`

Use a regular SDK project with ASP.NET shared framework available.

Practical options:

- `Microsoft.NET.Sdk` with `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
- or `Microsoft.NET.Sdk.Web` if you want the simpler ASP.NET surface

Recommendation:

- prefer `Microsoft.NET.Sdk`

Why:

- it reinforces that the project is not the executable host
- it still can contain controllers, MVC types, and SignalR when using `Microsoft.AspNetCore.App`

## Detailed Migration Plan

### Phase 1. Create the New Project

Create a new project:

- `FoodDiary.Presentation.Api/FoodDiary.Presentation.Api.csproj`

Initial references:

- `FoodDiary.Application`
- `FoodDiary.Contracts`
- optionally `FoodDiary.Domain` if needed for adapter-only types such as `UserId`
- ASP.NET Core shared framework

Do not add:

- `FoodDiary.Infrastructure`

Expected result:

- the project compiles as a non-host ASP.NET adapter library

### Phase 2. Move HTTP Adapter Infrastructure First

Move these first:

- base controllers
- `ResultExtensions`
- `UserExtensions`

Why first:

- all controllers depend on them
- it establishes the new base namespace before feature migration

Suggested new folders:

- `Controllers/`
- `Extensions/`

Expected result:

- controller base layer exists in the new project
- no controller still depends on types left behind in host

### Phase 3. Move SignalR Adapter Pieces

Move:

- `EmailVerificationHub`
- `EmailVerificationNotifier`
- `UserIdProvider`

Reason:

- these are web adapter concerns, not composition root concerns
- `EmailVerificationNotifier` depends directly on `IHubContext<EmailVerificationHub>`

Expected result:

- all hub-related types live together in presentation

### Phase 4. Move Feature Controllers

Move feature folders one by one from [`Features/`](./Features) into the new project.

Recommended order:

1. low-risk simple controllers
2. CRUD-style resource controllers
3. auth and admin controllers
4. dashboard and other endpoints with broader dependencies

This order reduces the blast radius of the first passes.

Expected result:

- the host no longer contains HTTP endpoints
- only presentation project contains controllers

### Phase 5. Introduce Presentation Registration Extensions

Add something like:

- `AddPresentationApi()`
- `MapPresentationApi()`

The host then becomes:

- host configures app-wide services
- host calls presentation registration
- host calls presentation endpoint mapping

This is the point where [`Extensions/ApiServiceCollectionExtensions.cs`](./Extensions/ApiServiceCollectionExtensions.cs) and [`Extensions/ApiApplicationBuilderExtensions.cs`](./Extensions/ApiApplicationBuilderExtensions.cs) should be split into:

- host composition extensions
- presentation registration extensions

Expected result:

- no MVC or SignalR type registration logic needs to live directly in the host project

### Phase 6. Simplify `FoodDiary.Web.Api`

After migration, `FoodDiary.Web.Api` should contain roughly:

- `Program.cs`
- host/composition extensions
- settings files
- launch settings
- maybe Swagger host wiring

Remove empty folders:

- `Controllers/`
- `Features/`
- `Hubs/`
- `Services/` if nothing host-specific remains

Expected result:

- `FoodDiary.Web.Api` becomes an actual composition root

### Phase 7. Update Tests

This refactor will affect several tests.

#### Architecture tests

Current tests reference `FoodDiary.Web.Api` directly:

- [`tests/FoodDiary.ArchitectureTests/LayeringTests.cs`](../tests/FoodDiary.ArchitectureTests/LayeringTests.cs)

They will need updates such as:

- host should reference presentation, application, infrastructure
- presentation should not be referenced by application/domain/infrastructure
- controller-folder assertions should move from host to presentation project

Likely new assertions:

- `FoodDiary.Presentation.Api` references `FoodDiary.Application`
- `FoodDiary.Presentation.Api` does not reference `FoodDiary.Infrastructure`
- `FoodDiary.Web.Api` references `FoodDiary.Presentation.Api`

#### Integration tests

Current tests rely on:

- [`ApiWebApplicationFactory.cs`](../tests/FoodDiary.Web.Api.IntegrationTests/TestInfrastructure/ApiWebApplicationFactory.cs)
- `WebApplicationFactory<Program>`

This should remain stable if `Program` stays in `FoodDiary.Web.Api`.

However:

- tests that directly import `FoodDiary.Web.Api.Extensions` will need to move to `FoodDiary.Presentation.Api`
- presentation-only unit tests may deserve a new test project later, but this is optional

### Phase 8. Update Solution and Documentation

Update:

- solution / solution filter
- project references
- architecture docs if any
- AGENTS guidance if you want explicit host-vs-presentation rules

## Risks and How to Control Them

### Risk 1. Circular references

This is the main structural risk.

Avoid:

- `FoodDiary.Presentation.Api -> FoodDiary.Web.Api`

Required shape:

- `FoodDiary.Web.Api -> FoodDiary.Presentation.Api`

### Risk 2. Half-moved adapter logic

Avoid leaving:

- controllers in presentation
- base controller and result mapping in host

That would make the separation awkward and fragile.

### Risk 3. Swagger and controller discovery breakage

Controller discovery will fail if:

- the new assembly is not loaded or not referenced by the host
- MVC registration is incomplete

Mitigation:

- make `AddPresentationApi()` responsible for `AddControllers()`
- ensure the host references the presentation assembly directly

### Risk 4. SignalR endpoint breakage

If hubs move, host endpoint mapping must still call the right assembly registration and mapping methods.

Mitigation:

- keep hub registration and mapping in the presentation registration package
- keep auth token extraction wiring in host auth config

### Risk 5. Test drift

Architecture tests currently encode the old structure.

Mitigation:

- update tests in the same refactor, not later

## What Success Looks Like

At the end of the refactor:

- `FoodDiary.Web.Api` contains almost no endpoint logic
- all controllers and API adapter helpers live in `FoodDiary.Presentation.Api`
- host remains the single executable entrypoint
- project dependencies point inward correctly
- architecture tests describe the new structure, not the old one
- integration tests still boot `Program` from the host project

## Recommended Execution Strategy

Do not move everything in one huge patch.

Preferred rollout:

1. add new project
2. move shared presentation infrastructure
3. move SignalR pieces
4. move controllers feature by feature
5. update host registration
6. update tests
7. remove obsolete files from host

This gives small checkpoints and makes it much easier to catch namespace or discovery issues early.

## Recommendation

Proceed with the refactor, but do it as a full adapter extraction, not a partial controller shuffle.

That means:

- new dedicated presentation project
- host remains executable and composition root
- all MVC/SignalR adapter concerns move together
- tests and architecture rules are updated in the same change stream

That is the cleanest version of the design you are aiming for.
