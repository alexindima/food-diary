# Products food quality

Own the shared FoodQualityScore calculation and FoodQualityGrade in their existing
CLR namespaces. Reference only Products Domain.Contracts and shared Domain.Primitives.
Do not reference aggregates, persistence, application or providers. Consumers reference
this project directly; Products retains ownership of the formula and ProductType input.
Preserve validation order, category modifiers, rounding and score/grade boundaries.
Existing formula cases remain in Products Domain.Tests alongside Product invariants.
