# Domain Review

Date: 2026-03-24
Scope: `FoodDiary.Domain`

## Summary

The domain layer is stronger than average. It already has:

- Strongly typed IDs
- Richer entities instead of pure anemic DTO-style models
- Validation for many range and normalization rules
- Domain events in important state transitions
- Good protection against `Guid.Empty` in many entity factories

The main risks are concentrated around `User`, time handling consistency, aggregate boundaries, and a few places where invariants are weaker than the rest of the model.

## Findings

### 1. UTC invariants are not fully enforced in `User`

Priority: High

Security-sensitive timestamps are named as UTC but are not consistently normalized before storage.

Examples:

- `SetEmailConfirmationToken(...)`
- `SetPasswordResetToken(...)`
- `MarkDeleted(...)`

Why it matters:

- A value may be passed with `Local` or `Unspecified` kind and stored as-is
- Expiration logic becomes fragile
- Soft-delete and token state can drift depending on caller behavior

References:

- `FoodDiary.Domain/Entities/Users/User.cs`

Relevant lines:

- `EnsureFutureUtc(...)` compares with `DateTime.UtcNow` but does not normalize input
- `MarkDeleted(DateTime deletedAtUtc)` stores incoming value directly

Recommendation:

- Introduce one explicit normalization helper for UTC timestamps
- Normalize and validate before assigning all `...Utc` fields
- Add tests for `Utc`, `Local`, and `Unspecified` inputs

### 2. `User` allows invalid floating-point values for profile measurements

Priority: High

`Weight` and `Height` are validated with `EnsurePositive(...)`, but that helper does not reject `NaN` or `Infinity`.

Why it matters:

- Breaks core domain invariants
- Can poison calculations, comparisons, persistence, and serialization
- Inconsistent with the rest of the domain, where finite checks are already common

References:

- `FoodDiary.Domain/Entities/Users/User.cs`

Relevant lines:

- `UpdateProfile(...)`
- `EnsurePositive(double? value, string paramName)`

Recommendation:

- Reject `double.NaN`, `double.PositiveInfinity`, and `double.NegativeInfinity`
- Add regression tests around `UpdateProfile(weight: ..., height: ...)`

### 3. `User` aggregate is overloaded

Priority: High

`User` currently contains:

- Authentication state
- Email confirmation and password reset state
- Telegram link state
- Profile data
- Nutrition and activity goals
- AI quota settings
- Dashboard preferences
- Soft-delete state
- Roles and many navigation collections

Why it matters:

- Makes the aggregate hard to reason about
- Increases risk of conflicting invariants
- Encourages “god aggregate” growth as the app evolves
- Raises the chance that unrelated features collide in the same type

References:

- `FoodDiary.Domain/Entities/Users/User.cs`

Recommendation:

- Short term: separate internal behavior into clearer sections/helpers/value objects
- Medium term: consider splitting responsibilities, especially auth/security state from profile/goals/preferences

### 4. Mutable navigation collections weaken aggregate boundaries

Priority: Medium-High

Several entities expose mutable `ICollection<T>` navigations publicly.

Examples:

- `User.Meals`, `User.Products`, `User.Recipes`, ...
- `Product.MealItems`, `Product.RecipeIngredients`
- `Recipe.MealItems`, `Recipe.NestedRecipeUsages`
- `Role.UserRoles`

Why it matters:

- External code can mutate associations outside aggregate behavior
- Domain invariants become easier to bypass
- The domain surface starts looking persistence-driven instead of behavior-driven

References:

- `FoodDiary.Domain/Entities/Users/User.cs`
- `FoodDiary.Domain/Entities/Products/Product.cs`
- `FoodDiary.Domain/Entities/Recipes/Recipe.cs`
- `FoodDiary.Domain/Entities/Users/Role.cs`

Recommendation:

- Prefer private lists with read-only exposure where behavior matters
- Keep ORM convenience from dominating domain design

### 5. Optional-field update semantics are inconsistent

Priority: Medium

Different aggregates use different rules for clearing optional fields.

Observed patterns:

- `Product` supports explicit clear flags like `clearBarcode`, `clearImageUrl`
- `Recipe` cannot explicitly clear some media fields through update methods
- `User.UpdateProfile(...)` treats `null` as “do not change”, while whitespace may normalize to empty string rather than `null`

Why it matters:

- Makes application-layer mapping more error-prone
- Creates inconsistent persisted data
- Complicates frontend/API contracts

References:

- `FoodDiary.Domain/Entities/Products/Product.cs`
- `FoodDiary.Domain/Entities/Recipes/Recipe.cs`
- `FoodDiary.Domain/Entities/Users/User.cs`

Recommendation:

- Standardize one policy:
  - either `null => clear`
  - or explicit `clearXxx` flags
- Use the same semantics across aggregates for comparable fields

### 6. Time handling is inconsistent across the domain

Priority: Medium

Some entities correctly normalize date-only values to `utc.Date`, while audit fields and domain events still use direct `DateTime.UtcNow`.

Examples:

- Good date-only normalization:
  - `Cycle`
  - `CycleDay`
  - `WeightEntry`
  - `WaistEntry`
- Direct clock usage:
  - `Entity.SetCreated()`
  - `Entity.SetModified()`
  - Domain event `OccurredOnUtc`
  - `RecentItem`
  - parts of `User`

Why it matters:

- Makes tests less deterministic
- Creates multiple “time policies” in the same domain
- Increases edge-case complexity around day boundaries and event timing

References:

- `FoodDiary.Domain/Common/Entity.cs`
- `FoodDiary.Domain/Events/*.cs`
- `FoodDiary.Domain/Entities/Recents/RecentItem.cs`

Recommendation:

- Decide whether the domain should own wall-clock time directly or receive normalized timestamps from callers
- Apply that policy consistently

### 7. `DailySymptoms` is a value object implemented as a mutable class

Priority: Medium

`DailySymptoms` behaves like a value object but is implemented as a mutable reference type with manual equality.

Why it matters:

- More code and more room for mistakes than necessary
- Less explicit immutability than a `record` or `record struct`
- Harder to maintain over time

References:

- `FoodDiary.Domain/ValueObjects/DailySymptoms.cs`

Recommendation:

- Consider converting it to an immutable record-based value object

### 8. Some state transitions are under-protected

Priority: Medium

`User` protects `Activate()` after delete, but many other mutations still appear allowed for deleted users.

Examples:

- Updating password
- Setting tokens
- Updating profile and goals

Why it matters:

- Soft-deleted state may not be a true protected state
- Business rules around deleted accounts can become inconsistent across application handlers

References:

- `FoodDiary.Domain/Entities/Users/User.cs`

Recommendation:

- Decide explicitly whether deleted users are mutable
- If not, enforce it in domain methods rather than relying on application-layer discipline

### 9. Some methods mark entities modified even when no value changed

Priority: Low-Medium

Example:

- `User.UpdateAiTokenLimits(...)` always calls `SetModified()`, even if neither limit changed

Why it matters:

- Inflates audit noise
- Can trigger unnecessary persistence writes

References:

- `FoodDiary.Domain/Entities/Users/User.cs`

Recommendation:

- Track actual change before setting `ModifiedOnUtc`

## Strong Areas

- `Product` has good identity/media/nutrition separation
- `Meal` is reasonably rich and protects child creation through aggregate methods
- `Cycle` and day-level tracking are much cleaner than average CRUD-style health-tracking models
- `WeightEntry` and `WaistEntry` validate date and numeric constraints well
- Strongly typed IDs are used consistently

## Recommended Fix Order

1. Fix UTC normalization in `User` and other UTC-named fields
2. Fix `NaN`/`Infinity` hole in `User.UpdateProfile(...)`
3. Add regression tests around user security timestamps and numeric profile fields
4. Standardize optional-field clearing semantics across aggregates
5. Reduce public mutable navigation exposure
6. Refactor `User` to reduce aggregate overload
7. Simplify `DailySymptoms` into a stricter immutable value object

## Notes

This review focused on model design, invariants, aggregate boundaries, and consistency. It was based on repository inspection rather than a full local build/test run.
