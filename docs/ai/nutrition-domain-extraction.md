# Nutrition domain ownership

Current ownership update: [ADR 0027](../adr/0027-retire-shared-domain-assemblies.md) supersedes the residual central/Nutrition assembly boundaries described below. The earlier extraction evidence remains historical.

Food quality scoring and food quantity units now have a narrow shared domain owner.
They are consumed by several food workflows and do not belong to one aggregate.

| Type | Physical owner |
| --- | --- |
| ProductType | Modules/Products/Domain.Contracts/Enums |
| FoodQualityScore and FoodQualityGrade | Shared/FoodDiary.Nutrition.Domain/ValueObjects |
| MeasurementUnit | Shared/FoodDiary.Nutrition.Domain/Enums |

Products Domain.Contracts owns identity and stable classification, references only
shared primitives, and contains no scoring algorithm or Product aggregate. Nutrition
Domain references that contract. Products Domain references Nutrition Domain for
food scoring and units. This direction does not create a cycle.

Nutrition Domain references shared Primitives for the public
FoodDiary.Domain.Primitives.DomainGuard.NonNegativeFinite and Defined methods.
The temporary central Domain reference and friend grant have been removed.
Validation bodies and exception contracts remain unchanged. Central Domain
continues to reference only shared primitives; there is no reverse dependency.
See `docs/ai/domain-guard-extraction.md`.

All four production types retain their original behavior, namespaces, member
signatures and numeric values. Scoring formula, ProductType modifiers, clamp, grade
thresholds, MidpointRounding.ToEven and validation-before-zero-calorie fallback are
unchanged. Product/meal/recipe mappings and persistence configurations are unchanged.
There are no HTTP, JSON, EF model, default, conversion, schema or data changes.
Consumers and all executable hosts must be rebuilt together; old binary
compatibility is not promised.

Focused FoodQualityScore cases move from AdditionalValueObjectsInvariantTests to
tests/FoodDiary.Nutrition.Domain.Tests under the existing Shared/Core test layout.
Mixed HealthArea/domain tests remain central. New focused regression cases cover
enum contracts, grade boundaries, modifiers, rounding, clamps and invalid inputs;
central application compatibility tests compare Product, Meal and Recipe mappings.
The dependency matrix, narrow ownership guards, solution and restore/source COPY
entries in the Web.Api, Initializer and JobManager Dockerfiles protect packaging.

This owner is limited to food scoring and food units. HealthArea types, visibility,
language/email/desired values, CycleId, DomainConstants and generic guard extraction
remain outside this change.
