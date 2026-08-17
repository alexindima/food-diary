# ADR 0012: Cycle Health-Data and Calendar-Date Boundary

- Status: Proposed
- Date: 2026-08-17
- Owners: Cycles, Privacy, and Product
- Related: ADR 0003, ADR 0004, ADR 0005, ADR 0006
- Supersedes: None

## Context

Cycle tracking combines local calendar dates, reproductive-health journal data, fertility signals, nutrition comparisons, and probabilistic predictions. The first implementation represented calendar days with UTC `DateTime` values and grouped goal, reproductive state, prediction visibility, and privacy preferences in one tracking mode. That shape creates timezone ambiguity, makes sensitive-data consent implicit, and makes safe evolution of predictions and exports difficult.

## Decision Drivers

- A day selected by a user must remain the same civil date in every timezone.
- Existing v1 routes and consumers require an explicit compatibility path.
- Fertility, sexual, nutrition-correlation, and export processing require purpose-scoped consent.
- Predictions must remain non-diagnostic, explainable, revisioned, and suppressible by reproductive state.
- Cross-feature access must continue through application contracts owned by the relevant module.

## Considered Options

1. Keep UTC `DateTime`, the combined tracking mode, and implicit consent.
2. Replace the v1 contract immediately with date-only payloads and a new profile schema.
3. Introduce date-only domain semantics and additive profile/privacy contracts while keeping a compatible v1 presentation adapter.

## Decision

Choose option 3.

- Domain, application, and persistence models use `DateOnly` for cycle calendar days. Presentation adapters temporarily accept existing date or date-time inputs and map them without timezone conversion; compatibility responses remain available while consumers migrate to ISO `yyyy-MM-dd` values.
- `CycleTrackingMode` remains a compatibility projection. New code stores goal and reproductive state separately; factors remain independent time-bounded facts.
- The Cycles aggregate owns purpose-scoped consent records. Fertility writes and Nutrition Insights require active matching consent. Revoking fertility consent stops processing and removes fertility-signal data through an explicit user action.
- Migration treats an existing fertility-signal history as prior contextual opt-in so current users do not lose access to data they deliberately recorded. Profiles without such history start with fertility processing disabled; Nutrition Insights is never enabled implicitly.
- Standard cycle export excludes fertility signals, sexual activity, and free-text notes. Sensitive export requires an explicit scope, a warning in the client, and current-password reauthentication.
- Cycle visibility on the dashboard and discreet-notification behavior are explicit profile preferences.
- Period predictions do not expose ovulation estimates in the safe default experience. Prediction revisions and deterministic historical calibration belong to the Cycles module and must include algorithm version, reason codes, and non-diagnostic wording.
- Pregnancy, postpartum, lactation, perimenopause, and no-period states use journal-only behavior and suppress period predictions until the user returns to a cycling state.

## Consequences

### Positive

- Cycle days no longer shift because of timezone conversion.
- Sensitive processing and exports become inspectable and revocable.
- Product state, goals, factors, and privacy preferences can evolve independently.
- Prediction changes can be explained and evaluated against historical coverage.

### Negative

- The transition requires additive API fields, migration backfills, and compatibility mapping.
- Sensitive export is unavailable to passwordless accounts until a password is configured.
- Consent and prediction-revision tables add persistence and test surface.

## Enforcement

- Domain and application tests cover date invariance, consent gates, state transitions, scoped deletion, export redaction, and calibration.
- Presentation and integration snapshots protect the additive v1 contract.
- Architecture tests keep Cycles, Export, Dashboard, and Users interactions behind application boundaries.
- Frontend tests and E2E scenarios cover consent prompts, journal-only states, export warnings, and mobile behavior.

## Follow-up

- Implement the migration and compatibility adapters.
- Update the living cycle documentation after the rollout contract is verified.
- Change this ADR to `Accepted` after product/privacy review.
