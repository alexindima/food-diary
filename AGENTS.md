# Repository Guidelines

## Project Structure & Module Organization
- `FoodDiary.Web.Client/` – Angular workspace; `src/` hosts the primary SPA, while shared UI components live in `projects/fd-ui-kit/`. Build artifacts drop under `dist/`.
- `.NET` solution (`FoodDiary.*`, `FoodDiary.Web.Api/`) sits in the repo root. Each layer (Domain, Application, Infrastructure, Web.Api) is a separate project referenced by `FoodDiary.sln`.
- Legacy Nest backend remains in `backend/food-diary.web.api/`. Only touch if you intend to update the existing Node stack.

## Build, Test, and Development Commands
- Frontend: `cd FoodDiary.Web.Client && npm install` to restore packages, `npm run build` for production build, `npm run lint` / `npm run test` for ESLint/Karma (lint currently fails until rule fixes land).
- UI Kit: `npx ng build fd-ui-kit` to bundle the library under `dist/fd-ui-kit`.
- Backend (.NET): `dotnet build FoodDiary.sln` compiles all layers; `dotnet run --project FoodDiary.Web.Api` starts the API.
- Legacy Nest backend: `cd backend/food-diary.web.api && npm run start:dev`.

## Coding Style & Naming Conventions
- TypeScript uses Angular defaults: strict compilation, standalone components, SCSS styles. Prefer `fd-ui-kit/...` imports over relative paths for shared UI pieces.
- Linting: ESLint (`eslint.config.js`), Stylelint, Prettier. Run `npm run lint`, `npm run stylelint`, `npm run prettier` before committing.
- C# follows standard .NET conventions with nullable enabled. Keep namespace per folder (e.g., `FoodDiary.Application.*`).

## Backend Architecture (.NET)
- Domain model uses strongly typed IDs/value objects for entities (avoid raw `Guid`/`int` in public surfaces).
- CQRS via MediatR: separate commands/queries with per-handler logic; controllers stay thin and delegate via mediator.
- Validation through MediatR pipeline behaviors + FluentValidation on commands/queries.
- Domain layer: aggregates enforce invariants; prefer factory/static creation methods; emit domain events where appropriate.
- Infrastructure: EF Core `DbContext` lives here; entity configuration via Fluent API; migrations are maintained in this project.
- API contracts: map DTOs to domain explicitly; nullable enabled; prefer structured results (`Result`/`OneOf`) over ad-hoc codes.
- Cross-cutting concerns (logging, tracing, retry policies) are applied via middleware/pipeline behaviors.

## Angular + TypeScript Best Practices
- Use strict typing, favor inference when obvious, and avoid `any` (prefer `unknown` when uncertain).
- Standalone components are the default in Angular v20+; do not set `standalone: true` manually.
- Prefer signals for local state and `computed()` for derived data; avoid `mutate`, use `update`/`set`.
- Use `input()`/`output()` helpers (components and directives) instead of decorators; keep components focused and small with `ChangeDetectionStrategy.OnPush`.
- Place host bindings/listeners in the `host` object, not via `@HostBinding`/`@HostListener`.
- Use lazy-loaded feature routes and `NgOptimizedImage` for static images (not for inline base64).
- Templates: use native control flow (`@if`, `@for`, `@switch`), avoid template arrow functions/regex/globals; prefer class/style bindings over `ngClass`/`ngStyle`.
- Services: single responsibility, `providedIn: 'root'` for singletons, `inject()` over constructor injection.
- Accessibility: meet WCAG AA, pass AXE checks, handle focus management and ARIA attributes explicitly.

## Testing Guidelines
- Angular unit tests via Karma/Jasmine: `npm run test`. Specs sit beside components (`*.spec.ts`).
- .NET tests aren't configured yet; if you add any, wire them into `dotnet test` and mention the project path.

## Commit & Pull Request Guidelines
- Use descriptive commit titles (imperative mood), e.g., “Add user dialog service” or “Fix Jwt refresh flow”.
- PRs should include: a concise summary, linked issue (if applicable), screenshots for UI-facing changes, and notes on testing (commands run, results). Ensure both `npm run build` and `dotnet build` succeed before requesting review.

### EF Core migrations
- При добавлении миграций всегда коммитьте оба файла: основной `*.cs` и `*.Designer.cs`, чтобы инструменты и проверки видели модель.
