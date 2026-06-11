# Shopping List Improvement Plan

## Goal
Turn shopping lists from a form-first checklist into a fast, plan-aware shopping workflow for the food diary product.

The target flow is:

1. Meal plan and recipes produce a deduplicated shopping list.
2. Each generated item keeps source context so the user understands why it is needed.
3. The shopping screen prioritizes fast capture, compact checking, and hiding completed items.
4. The data model leaves room for pantry, purchasable products, and household sharing without another rewrite.

## Current Gaps
- Manual add is centered on a multi-field form instead of one-line capture.
- Meal plan generation aggregates by product only and drops recipe/meal/day context.
- Shopping list updates replace all items, which makes item identity unstable.
- Items have only product/name/amount/unit/category/checked/sort fields.
- There is no pantry-aware deduction or post-shopping inventory loop.

## Phase 1: Backend Foundation
- Replace item replace-all behavior with stable item operations.
- Extend item data with note, aisle/category, checked metadata, and source-aware response fields.
- Add a source model for meal plan and recipe generated items.
- Keep custom free-text items supported.
- Regenerate EF migration from the simplified empty-client baseline.

## Phase 2: Meal Plan Import
- Generate shopping lists with source context.
- Deduplicate generated ingredients while preserving all source contributions.
- Include generated source labels in API responses.
- Add tests for source-aware aggregation and nested/recipe edge cases where practical.

## Phase 3: List-First UX
- Replace the large add form with one-line smart add plus optional details.
- Add compact shopping mode controls: hide checked, grouped view, quick check/uncheck.
- Show source tags for generated items.
- Preserve existing multiple-list management.

## Phase 4: Verification
- Update API snapshots when contracts change.
- Add or update application, presentation, and Angular tests.
- Run focused backend and frontend checks before merge.

## Deferred
- Pantry/inventory deduction.
- Household real-time collaboration.
- Barcode scanning, photos, delivery integrations, and budget totals.
