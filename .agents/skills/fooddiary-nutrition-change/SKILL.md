---
name: fooddiary-nutrition-change
description: Implement or review FoodDiary changes to nutrition quantities, grams and portions, automatic/manual totals, nested recipes, or imported nutrient data. Trace the numeric contract across UI, API, and owning domain rules; use for nutrition behavior changes rather than general health advice.
---

# FoodDiary nutrition change

Establish the numeric contract before changing a conversion or recalculation. Resolve references below against the current repository root and follow its applicable guides and Wiki change route.

## Identify the representations

Record each affected value's unit, basis, provenance, and precision: product nutrients per BaseAmount, ingredient amount in its product unit, recipe totals and serving count, meal recipe amount in servings, and any displayed gram equivalent. Determine whether a source is a saved snapshot or a live lookup before changing historical recalculation behavior.

Use current owning rules as authority:

- `Modules/Recipes/Domain/Nutrition/RecipeNutritionPolicy.cs` and `Modules/Recipes/Application/Services/RecipeNutritionCalculator.cs` for products, nested recipes, partial nutrition, and total rounding.
- `Modules/Meals/Domain/Entities/Meal.cs` and `Shared/FoodDiary.Nutrition.Contracts/Nutrition/Common/ManualNutritionLimits.cs` for stored meal totals and manual input limits.
- `FoodDiary.Web.Client/src/app/features/meals/lib/recipe-serving/recipe-serving-weight.service.ts` for available serving mass and lookup/cache behavior.
- `FoodDiary.Web.Client/src/app/features/recipes/components/manage/recipe-manage-lib/recipe-nutrition-form.manager.ts` and `FoodDiary.Web.Client/src/app/shared/lib/nutrition-form.utils.ts` for input basis changes and presentation precision.

Treat absent nutrition according to the owning contract. A missing ingredient, a stored zero, and a failed lookup may have different meanings. Preserve their distinctions through API mapping and UI labels.

## Select the meaningful invariants

Use applicable cases rather than repeating every case for every change:

- Scale a known product by its declared BaseAmount; scale a nested recipe by its serving count.
- Preserve total nutrition through recipe-to-portion-to-recipe changes, including fractional values and repeated toggles. Preserve input precision until the boundary where the owning implementation rounds.
- Check automatic/manual transitions, clearing optional inputs, and transient invalid or zero quantities without silently changing a previously valid basis.
- Offer a gram conversion only when the serving mass is known. Do not equate millilitres or pieces with grams without an explicit conversion contract. Current serving-weight code requires complete gram ingredient mass.
- Check incomplete recipes, unsupported units, lookup failure, and cache invalidation when recipe amounts or serving count change.
- Preserve the contract for historical meal snapshots, recipe edits, repeat/favorites, meal plans, and aggregate reports when those consumers are affected.

Use small independent numeric examples with an expected result derived before execution. A draft with 333.33 recipe calories and two servings should preserve the recipe total through a basis round trip; follow the actual display and storage rounding rules rather than forcing the same precision everywhere.

## Verify through the changed boundary

Navigate to existing tests rather than writing tests that restate the changed arithmetic:

- `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Domain.Tests/RecipeNutritionPolicyTests.cs`.
- `Modules/Recipes/tests/FoodDiary.Modules.Recipes.Application.Tests/RecipeNutritionCalculatorTests.cs`.
- Frontend serving-weight and recipe-nutrition-form-manager specs beside their implementations.
- The affected meal mapper/editor, lookup, or import tests found through current source search and Wiki test-plan.

For rendered checks, wait for the expected field or summary update after a mode change; reading immediately after a click can observe the old Angular render. Verify persistence through the real save/read/reopen boundary when authorized and relevant. Distinguish an unsaved draft conversion from a persisted meal result.

Report the representations and invariants checked, evidence on the final revision, and remaining affected consumers. If two layers disagree, establish the intended owning rule from current contracts/tests or ask the grounded product question; do not choose a new food calculation policy merely to make one test pass.
