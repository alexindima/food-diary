# Frontend Feature Guidelines

## Scope

Rules for `FoodDiary.Web.Client/src/app/features/`.

## Feature Layout

- Prefer these layer folders when applicable:
    - `api/`
    - `models/`
    - `contracts/` for deliberately published integration surfaces
    - `components/`
    - `dialogs/`
    - `lib/`
    - `pages/`
    - `resolvers/`
    - `<feature>.routes.ts`
- Keep feature internals inside the feature unless they are intentionally promoted to `shared/`.

## Boundaries

- Feature components/dialogs/lib/resolvers should use same-feature API/models or shared APIs.
- Do not reach into another feature page.
- Do not import another feature's route file.
- Do not import a feature root path directly; import a concrete layer.
- If multiple features need the same model/API helper, promote it to `src/app/shared`.
- For intentional cross-feature actions, publish a narrow typed capability in the owning feature's `contracts/` and bind its implementation at app composition. Do not expose the raw API client.
- Reusable UI and use-case facades may have explicit public contracts. Consumers must provide stateful facades at their own page/dialog scope. Contracts must not export route pages or foreign implementation files.
- Keep page-specific view types and pure mappers in `lib/` or `models/` when lower layers consume them; lower layers must not import `pages/`.

## UI

- Prefer `fd-ui-kit` components and shared app components before adding feature-local primitives.
- Keep page components orchestration-focused. Push repeated UI into `components/` and pure transformations into `lib/`.
- Keep templates simple enough to satisfy Angular ESLint template complexity rules.

## Localization

- Do not hardcode user-visible copy when the surrounding feature uses translation keys.
- Update both `assets/i18n/en/*.json` and `assets/i18n/ru/*.json` for copy changes.
