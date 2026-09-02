# Nutrition domain

Own exactly the shared food quality calculation (`FoodQualityScore` and
`FoodQualityGrade`) and food quantity `MeasurementUnit`. Preserve their existing
`FoodDiary.Domain.*` namespaces and numeric contracts. This is a subject-specific
domain library, not a destination for unrelated central Domain remnants.

Reference Products Domain.Contracts for stable ProductType classification, never
the Product aggregate. Products Domain may reference this library one-way.
Do not add application, persistence, transport, provider or host dependencies.

Reference shared Primitives for the public FoodDiary.Domain.Primitives.DomainGuard. Nutrition has no central Domain reference or friend grant. Central Domain must never reference Nutrition or Products contracts.

Keep formula, classification modifiers, clamp, grade thresholds, ToEven rounding,
validation exception types/parameter names and zero-calorie fallback unchanged.
Focused tests live in `tests/FoodDiary.Nutrition.Domain.Tests`; mixed consumer tests
retain their current owners. Assembly relocation requires coordinated rebuilds.
See `docs/ai/nutrition-domain-extraction.md`.
