# Nutrition domain

Own exactly the shared food quality calculation (`FoodQualityScore` and
`FoodQualityGrade`) and food quantity `MeasurementUnit`. Preserve their existing
`FoodDiary.Domain.*` namespaces and numeric contracts. This is a subject-specific
domain library, not a destination for unrelated central Domain remnants.

Reference Products Domain.Contracts for stable ProductType classification, never
the Product aggregate. Products Domain may reference this library one-way.
Do not add application, persistence, transport, provider or host dependencies.

The reference to central FoodDiary.Domain and its targeted InternalsVisibleTo
grant are temporary access to the existing internal DomainGuard. Do not copy its
validation or make it public. Remove both when generic guards move to their own
owner. Central Domain must never reference Nutrition or Products contracts.

Keep formula, classification modifiers, clamp, grade thresholds, ToEven rounding,
validation exception types/parameter names and zero-calorie fallback unchanged.
Focused tests live in `tests/FoodDiary.Nutrition.Domain.Tests`; mixed consumer tests
retain their current owners. Assembly relocation requires coordinated rebuilds.
See `docs/ai/nutrition-domain-extraction.md`.
