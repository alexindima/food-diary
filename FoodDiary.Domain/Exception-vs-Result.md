# Exception vs Result in `FoodDiary.Domain`

## Summary

The current approach in `FoodDiary.Domain` is mostly correct:

- Keep exceptions for invariant protection and invalid method arguments.
- Use `Result` or domain errors only for expected business rejections.
- Do not mix both styles inside the same type without a clear reason.

## Keep Exceptions

Exceptions are appropriate when the caller passes invalid data or attempts to put the aggregate/value object into an invalid state.

Typical cases in this project:

- `Create(...)` methods on entities and value objects.
- Validation of empty IDs, invalid ranges, empty required strings.
- Validation of `double` values such as `NaN`, `Infinity`, negative values, or zero when zero is not allowed.
- Argument-level API conflicts such as `clearX == true` together with a provided value.
- Internal consistency checks that should never fail in normal business flow.

Examples:

- [`FoodDiary.Domain/Entities/Meals/Meal.cs`](./Entities/Meals/Meal.cs)
- [`FoodDiary.Domain/Entities/Products/Product.cs`](./Entities/Products/Product.cs)
- [`FoodDiary.Domain/Entities/Recipes/Recipe.cs`](./Entities/Recipes/Recipe.cs)
- [`FoodDiary.Domain/Entities/Users/User.cs`](./Entities/Users/User.cs)
- [`FoodDiary.Domain/ValueObjects/DesiredWeight.cs`](./ValueObjects/DesiredWeight.cs)
- [`FoodDiary.Domain/ValueObjects/ProductNutrition.cs`](./ValueObjects/ProductNutrition.cs)

## First Candidates for `Result`

`Result` is useful when failure is an expected domain outcome, not a programmer error.

### 1. `User.Activate()`

File:

- [`FoodDiary.Domain/Entities/Users/User.cs`](./Entities/Users/User.cs)

Current behavior:

- Throws when activating a deleted user.

Why this is a candidate:

- "Deleted user cannot be activated directly" is a business rule.
- This can happen in normal application flow.
- A domain error such as `CannotActivateDeletedUser` is easier to handle than `InvalidOperationException`.

### 2. `Recipe.AddStep(...)`

File:

- [`FoodDiary.Domain/Entities/Recipes/Recipe.cs`](./Entities/Recipes/Recipe.cs)

Current behavior:

- Throws when `stepNumber` is already used inside the recipe.

Why this is a candidate:

- Duplicate step numbers are a normal conflict scenario from UI/API input.
- A domain error such as `DuplicateStepNumber(stepNumber)` is a clearer business response than `ArgumentException`.

## Borderline Cases

These do not need `Result` right now.

- Remove methods that already behave softly and do nothing if the target does not exist.
- Upsert-style methods that intentionally absorb normal repeated operations.

Examples:

- [`FoodDiary.Domain/Entities/Meals/Meal.cs`](./Entities/Meals/Meal.cs)
- [`FoodDiary.Domain/Entities/Recipes/Recipe.cs`](./Entities/Recipes/Recipe.cs)
- [`FoodDiary.Domain/Entities/Recipes/RecipeStep.cs`](./Entities/Recipes/RecipeStep.cs)

## `MealAiItemData.TryCreate(...)`

File:

- [`FoodDiary.Domain/Entities/Meals/MealAiItemData.cs`](./Entities/Meals/MealAiItemData.cs)

Current behavior:

- It catches its own `ArgumentException` / `ArgumentOutOfRangeException` and converts them into `bool + error`.

Why it stands out:

- It is not aligned with the rest of the domain model.
- It appears to be used only in tests.
- Right now it introduces a second style without a shared domain-wide contract.

Recommendation:

- Either keep only `Create(...)` and test with thrown exceptions.
- Or introduce a real shared `Result<T>` / domain error approach and use it consistently in production code.

## Recommended Rule for This Repository

Use this rule when adding or changing domain behavior:

- Throw exceptions for invariant violations, invalid input, invalid IDs, invalid ranges, and impossible internal states.
- Return `Result` only for expected business rejections that the application layer should handle explicitly.
- Prefer adding `Result` narrowly to specific methods instead of rewriting the whole domain model.

## Minimal Migration Path

If the team decides to introduce `Result`, start small:

1. Keep the existing exception-based validation for factories and value objects.
2. Introduce domain errors only for expected business failures.
3. First migrate:
   - `User.Activate()`
   - `Recipe.AddStep(...)`
4. Then decide whether `MealAiItemData.TryCreate(...)` should be removed or replaced with the same shared pattern.
