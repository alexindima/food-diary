# Domain Refactor Plan

Date: 2026-03-24
Scope: `FoodDiary.Domain`

## Goal

Strengthen domain invariants, reduce model drift, and make the core domain easier to evolve safely.

## Phase 1. Quick Fixes

Target: 1-2 days
Goal: close correctness gaps without broad redesign

### 1. Fix UTC normalization in `User`

Tasks:

- Normalize `expiresAtUtc` before storing in:
  - `SetEmailConfirmationToken(...)`
  - `SetPasswordResetToken(...)`
- Normalize `deletedAtUtc` in `MarkDeleted(...)`
- Make the validation helper explicitly require UTC semantics instead of only comparing against `DateTime.UtcNow`

Add tests for:

- `Utc` input
- `Local` input
- `Unspecified` input
- past/future boundary behavior

### 2. Fix invalid floating-point acceptance in `User`

Tasks:

- Update `EnsurePositive(...)` or replace it with a stricter helper
- Reject `NaN` and `Infinity` for:
  - `weight`
  - `height`

Add tests for:

- `double.NaN`
- `double.PositiveInfinity`
- `double.NegativeInfinity`
- valid positive values

### 3. Stop false audit updates

Tasks:

- Change methods that call `SetModified()` even when state did not change
- Start with:
  - `UpdateAiTokenLimits(...)`
  - any other similar methods discovered during test pass

Add tests for:

- `ModifiedOnUtc` unchanged when input does not change state

### 4. Review deleted-user mutation rules

Tasks:

- Decide whether deleted users may:
  - update password
  - set tokens
  - update profile
  - update goals
- If not allowed, add a guard and cover it with tests

Output of Phase 1:

- Safer `User` invariants
- Better timestamp correctness
- Less audit noise

## Phase 2. Consistency Pass

Target: 2-4 days
Goal: unify patterns across the domain

### 1. Standardize optional field update semantics

Choose one rule and apply it consistently:

- Option A: explicit `clearXxx` flags
- Option B: `null` means clear and a separate request model expresses "not supplied"

Recommended option:

- Keep explicit `clearXxx` flags inside domain methods where ambiguity exists

Priority targets:

- `User.UpdateProfile(...)`
- `Recipe.Update(...)`
- `Recipe.UpdateMedia(...)`
- any other aggregate with optional media/text fields

### 2. Standardize time normalization policy

Decide one domain-wide rule:

- date-only fields store UTC date
- timestamp fields store normalized UTC instant
- event timestamps follow the same rule consistently

Tasks:

- Audit all `DateTime.UtcNow` usage
- Audit all `NormalizeDate` and `NormalizeUtc` helpers
- Consolidate duplicate normalization logic where practical

### 3. Reduce public mutable collection exposure

Tasks:

- Replace public `ICollection<T>` where domain behavior matters with:
  - private `List<T>`
  - public `IReadOnlyCollection<T>`
- Keep only the aggregate methods as mutation points

Priority targets:

- `User`
- `Product`
- `Recipe`
- `Role`

Output of Phase 2:

- More consistent domain API
- Fewer ways to bypass aggregate rules
- Less persistence-driven leakage into the model

## Phase 3. `User` Aggregate Refactor

Target: 3-5 days
Goal: reduce overload in the most important aggregate

### Problem

`User` currently mixes:

- auth credentials
- email confirmation
- password reset
- profile data
- health metrics
- nutrition/activity goals
- Telegram binding
- AI quotas
- dashboard preferences
- deletion state

### Short-term refactor direction

Without changing persistence model immediately, split behavior inside the aggregate into clearer internal concepts.

Suggested internal extraction:

- `UserSecurityState`
  - refresh token
  - email confirmation token state
  - password reset token state
  - last login
- `UserProfile`
  - names
  - birth date
  - gender
  - language
  - profile image
  - dashboard layout
- `UserGoals`
  - calorie/macros
  - hydration/water
  - desired weight/waist
  - step goal
- `UserLifecycleState`
  - active/deleted state

Possible forms:

- internal helper methods first
- then value objects or owned submodels if it still improves clarity

### Refactor rule

- Do not split persistence just for aesthetics
- Split only where it reduces domain complexity and improves invariants

Output of Phase 3:

- Smaller conceptual surface inside `User`
- Easier tests
- Lower risk of future regressions

## Phase 4. Value Object Cleanup

Target: 1-2 days
Goal: simplify and harden value semantics

### 1. Rework `DailySymptoms`

Tasks:

- Consider converting to immutable `record` or `record struct`
- Keep validation at construction boundaries
- Preserve equality semantics with less boilerplate

### 2. Review duplication in numeric validation helpers

Tasks:

- Audit all `double` validation helpers
- Reduce copy-pasted finite/range checks where it improves clarity
- Avoid a giant generic helper if it harms readability

Output of Phase 4:

- Cleaner value object model
- Less repetitive validation code

## Test Plan

## Priority 1

- `User` timestamp normalization tests
- `User` invalid numeric input tests
- deleted-user mutation tests
- no-op update / audit timestamp tests

## Priority 2

- optional field clearing semantics tests
- aggregate collection mutation protection tests
- event timestamp expectations

## Priority 3

- `DailySymptoms` value semantics tests after refactor
- broad regression tests for `User` behavior after internal extraction

## Suggested Execution Order

1. Implement Phase 1 fully
2. Add missing tests before starting structural refactor
3. Execute Phase 2 consistency changes in small PRs
4. Refactor `User` only after behavior is covered by tests
5. Finish with value object cleanup

## Suggested PR Breakdown

### PR 1

- UTC normalization fixes
- invalid number fixes
- tests

### PR 2

- no-op audit fixes
- deleted-user guards
- tests

### PR 3

- optional field semantics cleanup
- collection exposure cleanup

### PR 4

- `User` aggregate internal refactor
- tests/regression pass

### PR 5

- `DailySymptoms` cleanup
- minor domain consistency cleanup

## Success Criteria

- All UTC-named fields are actually normalized UTC
- `User` rejects invalid numeric input
- Aggregate mutations happen through aggregate behavior, not public mutable collections
- `User` is easier to reason about and test
- Domain rules are more consistent across entities
