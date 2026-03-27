# Frontend Slice Plan

Date: 2026-03-27
Status: In progress
Parent plan: `ARCHITECTURE_10_10_PLAN.md`

## Goal

Move `FoodDiary.Web.Client/src/app` from global technical buckets to a feature-first structure that matches the backend shape and is safe to migrate incrementally.

## Current State

The frontend still relies mostly on global folders such as:

- `src/app/components`
- `src/app/services`
- `src/app/types`
- `src/app/resolvers`
- `src/app/guards`

This makes ownership blurry and increases cross-feature coupling.

The first safe pilot has already started:

- `src/app/features/products/product.routes.ts`
- `src/app/features/products/api/product.service.ts`
- `src/app/features/products/models/product.data.ts`
- `src/app/features/products/resolvers/product.resolver.ts`

Legacy paths still re-export these product files so the app keeps building while migration continues.

## Target Shape

Use this direction for the SPA:

- `src/app/features/<feature>/`
- `src/app/shared/ui/`
- `src/app/shared/api/`
- `src/app/shared/lib/`

Preferred feature layout:

- `features/<feature>/routes/` or feature-local `*.routes.ts`
- `features/<feature>/api/`
- `features/<feature>/models/`
- `features/<feature>/pages/`
- `features/<feature>/components/`
- `features/<feature>/dialogs/`
- `features/<feature>/resolvers/`

Use `shared/ui` for reusable presentational building blocks.
Use `shared/api` for cross-feature transport primitives only.
Use `shared/lib` for utilities, helpers, pure mappers, and non-visual reusable logic.

## Migration Rules

- Prefer moving one route-owning feature at a time.
- Keep route contracts stable while folders move.
- Use compatibility re-export shims only as a temporary bridge.
- Every compatibility shim must be tracked explicitly in this file and removed before its migration wave is considered complete.
- Do not move generic shared components into `features/*`.
- Do not create a giant `shared` bucket that simply replaces the old global buckets.
- If UI text changes, update both:
  - `FoodDiary.Web.Client/assets/i18n/en.json`
  - `FoodDiary.Web.Client/assets/i18n/ru.json`

## Order

### Wave 1

- [x] Start `products` feature slice with co-located routes, API adapter, models, and resolver
- [x] Move route-owned product UI from `components/product-container/*` into `features/products/*`
- [x] Reduce direct imports from legacy `services/types/resolvers` for product flows
- [x] Remove temporary product compatibility shims after imports are migrated

Tracked temporary compatibility shims for `products`:
- None remaining

### Wave 2

- [ ] Migrate `recipes`
- [ ] Migrate `shopping-lists`
- [ ] Migrate `hydration`

### Wave 3

- [ ] Migrate `dashboard`
- [ ] Extract clear `shared/ui`, `shared/api`, and `shared/lib` boundaries from what dashboard currently uses
- [ ] Reassess whether some current `components/shared/*` items belong in feature slices instead

### Wave 4

- [ ] Migrate remaining route features: `statistics`, `goals`, `weight-history`, `waist-history`, `cycle-tracking`, `premium`, `profile`
- [ ] Review `auth` flow structure separately from landing-page/public-shell structure
- [ ] Align admin frontend structure to the same rules

## Guardrails To Add Later

- [ ] Import-boundary lint rules for `features/*`
- [ ] Restrict direct cross-feature imports
- [ ] Restrict where route pages may import API adapters from
- [ ] Restrict what can import from `shared/ui` vs `shared/api` vs `shared/lib`

## Performance Follow-Up

The migration should also support these later cleanup goals:

- reduce bundle budget warnings
- reduce style budget warnings
- improve lazy-loading opportunities around route features

## Progress Log

### 2026-03-27

- Frontend structure audited.
- `products` selected as the first safe pilot instead of `dashboard`.
- Added `features/products` entrypoint files for routes, API, models, and resolver.
- Rewired `app.routes.ts` to consume feature-local product routes.
- Kept compatibility re-export shims in legacy product paths so the rest of the SPA can migrate incrementally.
- Tracked the temporary product shim files explicitly so they do not become permanent structure.
- Moved the route-owned product pages into `features/products/pages/*` so the product route tree no longer depends on legacy page source files under `components/product-container`.
- Moved product list/detail/filter source into `features/products/components/*` and rewired product pages plus `consumption-item-select` to import the feature paths instead of `product-container`.
- Converted the old product detail/list/filter TypeScript files into thin compatibility shims and explicitly tracked the remaining product bridges that still proxy legacy manage/dialog source.
- Promoted `base-product-manage` and the product dialogs to real feature-local source files under `features/products/*`.
- Converted the old `product-manage` and `product-list-dialog` TypeScript files into thin compatibility shims.
- Repointed the remaining product specs to feature-local imports and deleted the temporary legacy product component/dialog shim `.ts` files.
- Repointed all remaining imports away from the legacy `product.service`, `product.data`, and `product.resolver` shim files and deleted those shim files.
- Verified:
  - `npm run build`
  - `npm run lint`

## Next Recommended Task

Choose the next feature wave. `recipes` is the most natural follow-up because it already touches product-linked models and dialogs that were adjacent to the completed `products` slice.
