# Exercises Logical Module Guidelines

- Own exercise entries, validation, read-service implementation, repository ports/errors, stable read contracts, domain aggregate/enum/ID, EF mapping and repository under this module.
- Application preserves FoodDiary.Application.Exercises assembly identity and CLR namespaces. Contracts preserves the existing read-service/DTO CLR namespaces; Dashboard and TDEE reference Contracts only.
- Domain depends one-way on central Domain for User/UserId and internal DomainGuard (explicit IVT). User has no inverse Exercises navigation. Do not move User or make Domain depend on Exercises.
- Central Errors.Exercise is a compatibility facade; module Abstractions must not reference central Abstractions.
- Keep calories/rounding, validation, UTC/date normalization, cancellation and user access unchanged.
- Infrastructure owns AddExercisesModule; Application owns AddExercisesApplication. The shared DbContext explicitly calls ApplyExercisesPersistenceModel. Keep migrations and model snapshot central.
- Focused Application and Domain tests live under tests here. Shared database/HTTP/host/cross-module suites stay central. TrackingAndMealPlanCoverageGapTests contains no Exercises tests at the extraction base.
- Use root test conventions: exclude test helpers from coverage; never collect coverage during extraction validation.

Users owns the complete User aggregate, all credential/security partials, roles,
role audit and weight/waist goals under `Modules/Users/Domain`. `UserId` lives in
`Modules/Users/Domain.Contracts`, depending only on shared primitives. Consumers
reference the exact owner; central Domain retains shared guards and values without
an aggregate re-export. Authentication flows/providers, combined UserRepository,
DbContext, migrations and snapshot retain their existing owners. CLR namespaces,
security behavior and EF/HTTP contracts are unchanged. See
`docs/ai/users-domain-extraction.md` for residual seams and verification evidence.
