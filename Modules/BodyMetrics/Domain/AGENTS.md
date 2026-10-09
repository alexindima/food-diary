# BodyMetrics Domain

Keep `WeightEntry`, `WaistEntry`, `WeightEntryId`, and `WaistEntryId` here under `FoodDiary.Modules.BodyMetrics.Domain` with project-relative namespaces. Reference Users Domain.Contracts for `UserId`, DesiredWeightKg and DesiredWaistCm, and the exact module or FoodDiary.Domain.Primitives owner for values and guards. Preserve scalar foreign keys, validation, UTC date normalization, and audit timestamp behavior. Do not add reverse `User.WeightEntries` or `User.WaistEntries` navigations. Weight and waist goals belong to Users Domain.

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.

Use owner-local `MeasurementDay` for typed measurement mutations (`CreateForDay`/`UpdateDetails`). It represents a calendar `DateOnly`, with explicit conversion to the existing UTC-midnight DateTime encoding. Primitive compatibility adapters retain Local-to-UTC date extraction and Unspecified calendar-day preservation. Keep mapped DateTime fields and PostgreSQL date columns unchanged; a measurement day is not a local-midnight instant. Default/MinValue is representable as before; do not add implicit conversions or new date-range restrictions.

Use CreateWithMeasurement/UpdateMeasurement with Users Domain.Contracts MeasuredWeightKg or MeasuredWaistCm and owner MeasurementDay. Stored WeightKg/CircumferenceCm remain doubles. Compatibility Create/CreateForDay/Update/UpdateDetails retain scalar validation and temporal ordering; no-op changes preserve audit stamps. Do not reuse a desired goal type for a measured observation.
