# Role and food-quality boundaries

RoleNames now belongs to Users Domain.Contracts. Admin, Identity and Dietologist
Application reference that owner directly, removing their Users.Domain references.
Role entities and membership mutation remain Users-owned; authorization strings,
case comparison, bootstrap and impersonation rules are unchanged.

FoodQualityScore and FoodQualityGrade now belong to Products/FoodQuality, assembly
FoodDiary.Modules.Products.FoodQuality. This is a Products-owned reusable calculation,
not a shared contract bucket: it accepts ProductType and depends only on Products
Domain.Contracts and Domain.Primitives. Domain consumes FoodQuality one-way.
Dashboard, Favorites, Meals and Recipes Application no longer reference Products.Domain.
Actual scoring consumers, including tests, reference the new owner directly.
Infrastructure references needed for existing foreign read projections remain.

All three moved source files retain exact contents and namespaces. Score validation,
category modifiers, zero-calorie default, rounding and grade thresholds are unchanged.
No DI, database schema, EF mapping, HTTP payload or authorization policy changes.
Rebuild hosts together because CLR assembly ownership changes; binary compatibility
with old consumer assemblies is not promised.

The exact dependency matrix and owner tests guard the move. Existing Products
Domain.Tests retain all calculation cases and Product invariants without duplication.
RoleAndQualityBoundaryTests prevents the seven broad Application references from
returning and restricts the calculation assembly to its two scalar dependencies.
Verification evidence is recorded in .artifacts/role-quality-verification/README.md.
