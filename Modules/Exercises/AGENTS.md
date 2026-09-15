# Exercises Logical Module Guidelines

- Own exercise entries, validation, read-query handlers, repository ports/errors, stable read contracts, domain aggregate/enum/ID, EF mapping and repository under this module.
- Application and Contracts use canonical FoodDiary.Modules.Exercises identities and folder namespaces. Dashboard and TDEE dispatch owner requests from Contracts.
- Domain depends one-way on Users Domain.Contracts for UserId and shared Primitives for public DomainGuard. User has no inverse Exercises navigation. Do not move User or make Domain depend on Exercises.
- ExerciseErrors and its callers belong to this module. The central Errors.Exercise facade is retired; module Abstractions must not reference central Abstractions. Preserve error codes, messages and kinds; see docs/ai/measurement-error-facades.md.
- Keep calories/rounding, validation, UTC/date normalization, cancellation and user access unchanged.
- Infrastructure owns AddExercisesModule; Application owns AddExercisesApplication. The shared DbContext explicitly calls ApplyExercisesPersistenceModel. Keep migrations and model snapshot central.
- Focused Application and Domain tests live under tests here. Shared database/HTTP/host/cross-module suites stay central. TrackingAndMealPlanCoverageGapTests contains no Exercises tests at the extraction base.
- Use root test conventions: exclude test helpers from coverage; never collect coverage during extraction validation.

Users owns the complete User aggregate, all credential/security partials, roles,
role audit and weight/waist goals under `Modules/Users/Domain`. `UserId` lives in
`Modules/Users/Domain.Contracts`, depending only on shared primitives. Consumers
reference the exact owner; shared guards and generic values belong to
`FoodDiary.Domain.Primitives`; module-specific values stay with their owner. Authentication flows/providers, combined UserRepository,
DbContext, migrations and snapshot retain their existing owners. Security behavior and EF/HTTP contracts remain unchanged. See
`docs/ai/users-domain-extraction.md` for residual seams and verification evidence.

ExercisesDbContext is the third owned runtime context (ADR 0040). It shares the scoped connection and coordinated unit of work; the central schema and purge/read bridges remain unchanged.

Use the canonical project name as the namespace root and match folders. Projects are siblings. Public owner use cases are Contracts requests dispatched through ISender; keep outbound source ports and reusable algorithms separate. Preserve authorization, cancellation, wire shapes and persistence semantics.
