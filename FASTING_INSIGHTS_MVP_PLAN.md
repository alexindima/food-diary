# Fasting Insights MVP Plan

## Goal
Turn fasting check-ins into useful feedback, not just stored logs.

The first version should answer simple questions:
- which fasting sessions feel easier;
- whether risky symptoms repeat;
- whether the current fast may need extra caution.

## Product Outcome
After a user logs a few fasting check-ins, the fasting page should surface short insights such as:
- shorter fasts seem easier than longer ones;
- dizziness or weakness keeps repeating;
- the current fast looks rough and may be worth ending early.

## MVP Scope
Frontend-only insights based on existing fasting history and the current session.

No new backend endpoints.
No background jobs.
No notifications yet.

## Inputs
Use fields already available in fasting history:
- `plannedDurationHours`
- `checkInAtUtc`
- `hungerLevel`
- `energyLevel`
- `moodLevel`
- `symptoms`
- `checkInNotes`
- `status`

## Insight Rules
### 1. Current safety warning
Show when the current active fast has a risky check-in:
- `energyLevel <= 2` or `moodLevel <= 2`
- and symptoms include `dizziness` or `weakness`

Output:
- warning insight
- short copy suggesting to pause, hydrate, or end the fast if symptoms continue

### 2. Shorter vs longer fasting tolerance
Compare completed check-ins:
- shorter group: `plannedDurationHours < 24`
- longer group: `plannedDurationHours >= 24`

If both groups have at least 2 check-ins and the average energy or mood for shorter fasts is at least 1 point higher:
- show insight that shorter fasts seem easier to tolerate

### 3. Repeating symptom pattern
If a symptom appears in at least 2 check-ins:
- show recurring symptom insight
- prioritize `dizziness`, `weakness`, `headache`, `irritability`, `cravings`

### 4. Positive adherence / tolerance
If the user has at least 3 check-ins with:
- `energyLevel >= 4`
- `moodLevel >= 4`
- and no risky symptoms

Show positive insight:
- current fasting setup seems well tolerated

## UI Placement
Add a new card on the fasting page:
- title: `Insights`
- positioned between statistics and history

Each insight should have:
- short title
- one-line explanation
- tone: positive, neutral, or warning

## Non-Goals
Do not add:
- medical claims
- diagnosis language
- automatic fasting termination
- push reminders
- personalized protocol recommendations beyond simple pattern matching

## Implementation Plan
### Phase 1
- add local insight model on the fasting page
- compute insights from `history()` and `currentSession()`
- render up to 3 insights in a dedicated card
- add localization keys in English and Russian

### Phase 2
- add stronger safety copy for risky current-session combinations
- refine repeated symptom prioritization

### Phase 3
- move insight calculation to a shared service or backend if needed
- add trends over larger time windows
- add reminders or check-in prompts

## Acceptance Criteria
- no new API endpoints are required for MVP;
- insights render when enough check-in data exists;
- warning insight appears for risky current-session combinations;
- empty state is shown when there is not enough data;
- both `en.json` and `ru.json` are updated.
