# Fasting Check-Ins Refactor Plan

## Goal

Move fasting check-ins from a single mutable snapshot on `FastingOccurrence` to a dedicated history table so one fasting session can contain multiple check-ins.

## Model

- `FastingOccurrence` remains the fasting session aggregate.
- `FastingCheckIn` stores individual check-in entries.
- `FastingOccurrence` keeps latest check-in summary fields as denormalized read data for fast access:
  - `CheckInAtUtc`
  - `HungerLevel`
  - `EnergyLevel`
  - `MoodLevel`
  - `Symptoms`
  - `CheckInNotes`

## MVP Scope

1. Add `FastingCheckIn` entity and persistence.
2. Backfill existing single-check-in data from `FastingOccurrences` into `FastingCheckIns`.
3. Save new check-ins as append-only history entries.
4. Keep updating latest summary fields on `FastingOccurrence`.
5. Return `checkIns` in current/history API responses.
6. Show nested check-ins in fasting session history.
7. Add simple client-side `Load more` for history sessions.
8. Expose last saved check-in time in fasting telemetry summary.

## Follow-Up

- Replace `Сохранить отметку` with wording closer to `Добавить отметку`.
- Add server-side pagination for fasting history if history size grows.
- Rework fasting insights to analyze full `FastingCheckIns` history instead of latest occurrence snapshot only.
- Consider removing legacy latest-check-in columns from `FastingOccurrences` after all readers switch to the new model.
