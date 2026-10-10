# Verified FoodDiary boundary examples

## Frontend decoder

Source: `FoodDiary.Web.Client/src/app/features/hydration/api/hydration.service.ts`.
Check: `FoodDiary.Web.Client/src/app/features/hydration/api/hydration.service.spec.ts`.

```typescript
function hydrationEntryFromSdk(response: HydrationEntryHttpResponse): HydrationEntry {
    const value = requireSdkFields(response, ['id', 'timestampUtc', 'amountMl']);
    return { ...value, id: entityId<'hydration-entry'>(value.id), timestampUtc: utcInstant(value.timestampUtc) };
}
```

The SDK returns the native wire DTO. The adapter checks declared fields and
decodes the owning identity and instant without changing their runtime values.
The published `HydrationActions` capability retains the resulting entry model.
For another owner, inspect its actual contract; do not copy hydration meaning.

## Authorized backend handler

Source: `Modules/Hydration/Application/Commands/CreateHydrationEntry/CreateHydrationEntryCommandHandler.cs`.
Check: `Modules/Hydration/tests/FoodDiary.Modules.Hydration.Application.Tests/HydrationFeatureTests.cs`.

The handler resolves `UserId` through `CurrentUserAccessResolver`, returns failure
before writes when access fails, validates the amount, normalizes the instant,
creates `HydrationEntry` using `HydrationAmount`, and calls the owning write port
with the cancellation token. This is a concrete example of authorization and
semantic decoding before persistence. Follow the current owner guide when another
flow has different invariants.

## Evaluating this skill

`node scripts/ai/agent-trials.mjs run <case> control` and the same command with
`api-skill` prepare separate immutable snapshots and retain actual outcomes.
`compare` reports matched cohorts; small samples remain insufficient evidence.
Grader self-tests validate the grading machinery and never count as model runs.
