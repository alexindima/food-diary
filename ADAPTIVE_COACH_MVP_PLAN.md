# Adaptive Coach MVP Plan

## Current State

The repository already has the core pieces needed for an Adaptive Coach MVP:

- existing TDEE insight query and calculator:
  - `FoodDiary.Application/Tdee/Queries/GetTdeeInsight/`
  - `FoodDiary.Application/Tdee/Services/TdeeCalculator.cs`
  - `FoodDiary.Presentation.Api/Features/Tdee/`
  - `FoodDiary.Web.Client/src/app/features/dashboard/components/tdee-insight-card/`
- existing weekly review surface:
  - `FoodDiary.Web.Client/src/app/features/weekly-check-in/`
  - `FoodDiary.Presentation.Api/Features/WeeklyCheckIn/`
- existing goals editing flow:
  - `FoodDiary.Presentation.Api/Features/Goals/`
  - `FoodDiary.Web.Client/src/app/features/goals/`
- existing weight and meal data sources:
  - `FoodDiary.Application/WeightEntries/`
  - `FoodDiary.Application/Consumptions/`

This means the best direction is not to build a brand new "coach" subsystem from scratch.

Instead:

- keep `Tdee` as the calculation core for MVP
- evolve it into a more explicit coaching read model
- surface the result in `dashboard`, `weekly-check-in`, and `goals`

## Goal

Ship an MVP that answers these questions for the user:

1. What is happening with my weight trend?
2. What is my likely real maintenance intake right now?
3. Should I keep, raise, or lower my calorie target?

The MVP should:

- use deterministic logic only
- be conservative with recommendations
- explain the result in plain language
- never auto-change user goals without confirmation

## Product Shape

The MVP should produce one coaching insight for a rolling 14-day window.

Output:

- estimated TDEE
- adaptive TDEE
- weekly weight trend
- current calorie target
- suggested calorie target
- recommendation action
- confidence
- human-readable reasons
- insufficient-data state when logging quality is too low

Recommended actions:

- `keep_target`
- `increase_calories`
- `decrease_calories`
- `insufficient_data`

## Recommended Direction

Do not introduce a separate `Coach AI` or LLM-based flow in MVP.

Do not create a heavy new domain area unless the current `Tdee` model becomes too restrictive.

For MVP, the cleanest path is:

1. extend the existing `TdeeInsightModel`
2. add a small coaching calculator/explainer layer near `Tdee`
3. expose the result through the current TDEE endpoint or a thin adjacent coaching endpoint
4. integrate the result into weekly check-in and goals

## Proposed Read Model

Add explicit coaching fields on top of the current TDEE insight.

Suggested model shape:

```csharp
public sealed record AdaptiveCoachInsightModel(
    double? EstimatedTdee,
    double? AdaptiveTdee,
    double? CurrentCalorieTarget,
    double? SuggestedCalorieTarget,
    double? WeightTrendPerWeek,
    string RecommendationAction,
    int RecommendationDeltaCalories,
    string Confidence,
    int DataDaysUsed,
    IReadOnlyList<string> Reasons,
    bool HasInsufficientData);
```

MVP note:

- this can replace or extend `TdeeInsightModel`
- avoid creating both `TdeeInsightModel` and `AdaptiveCoachInsightModel` if they largely duplicate each other

## Calculation Logic

### Window

Use a rolling `14-day` window for MVP.

Why:

- less noisy than 7 days
- much faster feedback than 28 days
- already compatible with current adaptive TDEE logic

### Inputs

Use only these inputs in MVP:

- logged food intake
- weight entries
- current calorie target
- desired weight

Do not include in MVP:

- cycle data
- hydration
- fasting
- satiety
- wearables
- sleep
- AI-generated behavioral advice

### TDEE Formula

Reuse the current logic in `TdeeCalculator` as the base:

- average logged intake
- optional exercise burn if already available
- weight trend over time
- convert weight change to caloric surplus/deficit with `7700 kcal per kg`

Current implementation already does most of this in:

- `FoodDiary.Application/Tdee/Services/TdeeCalculator.cs`

### Recommendation Rules

Keep the first version intentionally conservative.

Suggested rules:

- if data quality is insufficient:
  - action = `insufficient_data`
- if `suggested target - current target` is between `-50` and `+50`:
  - action = `keep_target`
- if current target is too low relative to adaptive TDEE for a weight-loss goal:
  - action = `increase_calories`
- if current target is too high relative to adaptive TDEE for a weight-loss goal:
  - action = `decrease_calories`
- if user is effectively at maintenance and trend is stable:
  - action = `keep_target`

Clamp recommendation size:

- default delta step: `100 kcal`
- allow `150 kcal` only when the mismatch is clearly large
- never recommend a larger adjustment in MVP

### Insufficient Data Rules

The result should explicitly return `insufficient_data` when confidence is too weak.

Suggested thresholds:

- fewer than `8` days with logged calories in the 14-day window
- fewer than `4` weight entries in the 14-day window
- fewer than `14` actual days between earliest and latest useful data points

This is stricter than "show anything possible" and is better for trust.

## Explanation Layer

Add a deterministic explanation builder.

Suggested service:

- `FoodDiary.Application/Tdee/Services/TdeeInsightExplainer.cs`

Responsibility:

- convert raw numbers into short reason codes or messages
- keep explanation logic out of the controller and UI

Examples:

- `coach.reason.insufficient_weight_data`
- `coach.reason.insufficient_food_logs`
- `coach.reason.weight_loss_slower_than_expected`
- `coach.reason.weight_loss_faster_than_expected`
- `coach.reason.current_target_close_to_maintenance`
- `coach.reason.current_target_matches_goal`

For MVP, keep reasons as stable codes in the API and map them to localized strings in the frontend.

## Backend Plan

### Phase 1. Evolve the Existing TDEE Insight

Update:

- `FoodDiary.Application/Tdee/Models/TdeeInsightModel.cs`
- `FoodDiary.Application/Tdee/Services/TdeeCalculator.cs`
- `FoodDiary.Application/Tdee/Queries/GetTdeeInsight/GetTdeeInsightQueryHandler.cs`
- `FoodDiary.Presentation.Api/Features/Tdee/Responses/TdeeInsightHttpResponse.cs`
- `FoodDiary.Presentation.Api/Features/Tdee/Mappings/TdeeHttpMappings.cs`

Add:

- explicit recommendation action
- recommendation delta
- reasons collection
- insufficient-data marker

Expected outcome:

- dashboard can render a more useful coach card
- API can distinguish "no data" from "keep current target"

### Phase 2. Add Coaching Explanation Rules

Create:

- `FoodDiary.Application/Tdee/Services/TdeeInsightExplainer.cs`

Optional split if needed:

- `TdeeRecommendationCalculator.cs`
- `TdeeExplanationBuilder.cs`

Expected outcome:

- the query handler becomes thinner
- recommendation logic is testable
- UI text is driven by stable reason codes

### Phase 3. Add Goal Apply Flow

For MVP, avoid automatic goal changes.

Instead:

- add an explicit apply action from dashboard/goals/weekly check-in
- reuse the current goals update flow

Recommended approach:

- do not create a special database table yet
- translate "apply recommendation" into the existing `UpdateGoals` command

Possible addition:

- a lightweight command in `Tdee` or `Coaching` that internally calls the same goal update path

Example:

- `ApplyAdaptiveCoachRecommendationCommand`

Expected outcome:

- one-click goal adoption
- no duplication of goal-write logic

### Phase 4. Optional Recommendation History

If implementation remains small, add a history record later.

Do not include this in MVP unless it is needed immediately for product trust.

Possible later entity:

- `CoachRecommendationSnapshot`

Stored fields:

- user id
- date window
- adaptive TDEE
- current target
- suggested target
- action
- accepted/rejected

For MVP this is optional.

## Frontend Plan

### Phase 1. Upgrade the Existing TDEE Card

Reuse:

- `FoodDiary.Web.Client/src/app/features/dashboard/components/tdee-insight-card/`

Add support for:

- recommendation action
- reasons
- insufficient-data state
- clearer CTA text

Expected UI states:

- loading
- insight with keep recommendation
- insight with increase/decrease recommendation
- insufficient-data guidance

### Phase 2. Weekly Check-In Integration

Extend:

- `FoodDiary.Web.Client/src/app/features/weekly-check-in/models/weekly-check-in.data.ts`
- `FoodDiary.Web.Client/src/app/features/weekly-check-in/lib/weekly-check-in.facade.ts`
- `FoodDiary.Web.Client/src/app/features/weekly-check-in/pages/weekly-check-in-page.component.*`

Recommended UX:

- show a `Coach Review` block in weekly check-in
- summarize:
  - average intake
  - weight trend
  - suggested target
  - 2-3 reasons
- show `Apply recommendation` button

Expected outcome:

- weekly check-in becomes the main surface for coaching

### Phase 3. Goals Integration

Extend:

- `FoodDiary.Web.Client/src/app/features/goals/models/goals.data.ts`
- `FoodDiary.Web.Client/src/app/features/goals/lib/goals.facade.ts`
- `FoodDiary.Web.Client/src/app/features/goals/pages/goals-page.component.*`

Recommended UX:

- show current coach suggestion near calorie target settings
- show delta, for example `Suggested: 2050 kcal (-100)`
- provide one-click apply

Expected outcome:

- recommendation can be acted on where the user already edits targets

## API Design

Preferred short-term approach:

- keep the current TDEE endpoint and enrich its response

Existing path:

- `FoodDiary.Presentation.Api/Features/Tdee/TdeeController.cs`

Why:

- lowest implementation cost
- no need to maintain two similar read models
- dashboard already depends on this path

Alternative only if the model becomes too broad:

- create `GET /api/v1/coaching/adaptive-insight`

For MVP, prefer the existing TDEE endpoint unless the payload becomes confusing.

## Suggested DTO Additions

Add fields like:

- `RecommendationAction`
- `RecommendationDeltaCalories`
- `Reasons`
- `HasInsufficientData`

Example HTTP response:

```json
{
  "estimatedTdee": 2280,
  "adaptiveTdee": 2360,
  "bmr": 1710,
  "currentCalorieTarget": 2150,
  "suggestedCalorieTarget": 2050,
  "weightTrendPerWeek": -0.12,
  "confidence": "medium",
  "dataDaysUsed": 12,
  "goalAdjustmentHint": "hint.review_goals",
  "recommendationAction": "decrease_calories",
  "recommendationDeltaCalories": -100,
  "hasInsufficientData": false,
  "reasons": [
    "coach.reason.weight_loss_slower_than_expected",
    "coach.reason.current_target_close_to_maintenance"
  ]
}
```

## Testing Plan

### Application Tests

Add focused tests for:

- enough data -> recommendation returned
- too few logs -> insufficient data
- too few weights -> insufficient data
- losing too slowly -> decrease calories
- losing too fast -> increase calories
- maintenance on track -> keep target
- recommendation delta is clamped

Most important target:

- calculation and explanation services

### API Tests

Add coverage for:

- response shape with new fields
- mapping of reason codes
- insufficient-data payload

### Frontend Tests

Add or update tests for:

- `tdee-insight-card`
- `weekly-check-in.facade`
- `goals.facade`

Verify:

- card renders recommendation states correctly
- apply button emits only when recommendation exists
- insufficient-data state shows guidance rather than fake advice

## Non-Goals for MVP

Do not include these yet:

- automatic goal changes without user confirmation
- macro auto-adjustment beyond a simple calorie recommendation
- cycle-aware coaching
- sleep-aware coaching
- satiety-aware coaching
- wearable-aware coaching
- dietologist approval flows
- LLM-generated free-text coaching
- long-term recommendation history UI

## Risks

### Incomplete Food Logging

Risk:

- adaptive TDEE and recommendation become misleading if the user logs only part of meals

Mitigation:

- strict insufficient-data rules
- visible confidence label
- conservative adjustments only

### Noisy Weight Data

Risk:

- random day-to-day fluctuation creates unstable recommendations

Mitigation:

- keep smoothing in `TdeeCalculator`
- use 14-day window
- limit recommendation step size

### User Trust

Risk:

- recommendation feels arbitrary

Mitigation:

- always show why
- never auto-apply
- keep the advice explainable and stable

## Suggested Implementation Order

1. Extend the current TDEE read model with recommendation fields.
2. Extract explanation and recommendation rules into small testable services.
3. Update the TDEE API response and frontend dashboard card.
4. Add `Apply recommendation` through the existing goals update flow.
5. Integrate the same insight into weekly check-in.
6. Add frontend localization strings for all reason codes and actions.
7. Add tests around calculation thresholds and UI states.

## Candidate End State for MVP

The likely best end state for this iteration is:

- one adaptive calculation core built on top of the current `Tdee` feature
- one enriched insight response
- one-click apply through the existing goals write path
- dashboard as the quick view
- weekly check-in as the main coaching surface
- goals as the control surface

That gives a meaningful product upgrade without introducing unnecessary architecture or speculative AI complexity.
