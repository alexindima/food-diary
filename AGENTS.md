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

## Testing Guidelines
- Angular unit tests via Karma/Jasmine: `npm run test`. Specs sit beside components (`*.spec.ts`).
- .NET tests aren’t configured yet; if you add any, wire them into `dotnet test` and mention the project path.

## Commit & Pull Request Guidelines
- Use descriptive commit titles (imperative mood), e.g., “Add user dialog service” or “Fix Jwt refresh flow”.
- PRs should include: a concise summary, linked issue (if applicable), screenshots for UI-facing changes, and notes on testing (commands run, results). Ensure both `npm run build` and `dotnet build` succeed before requesting review.
