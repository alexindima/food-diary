# Exercises extraction inventory

Base: 3851a98e0340fd12191c56cf41f261a678a632c8; clean detached worktree, branch codex/exercises-module-extraction.

- Application: Create/Update/DeleteExerciseEntry and GetExerciseEntries slices, validators, input validation, enum/id parsers, mappings, ExerciseEntryReadService and DI. Preserve application assembly and namespaces.
- Contracts: IExerciseEntryReadService and ExerciseEntryModel; Dashboard and TDEE are actual application consumers, despite Wiki labeling them host/adapters.
- Abstractions: four repository interfaces, ExerciseEntryReadModel and ExerciseErrors. Central Errors.Exercise partial facade delegates to module errors; retain facade centrally and add one-way central Abstractions -> module Abstractions edge. Module Abstractions references Domain/Results only.
- Domain: ExerciseEntry, ExerciseEntryId, ExerciseType. No domain events specific to Exercises. User navigation has WithMany() with no inverse navigation; rg of central Domain shows no other Exercises consumers. DomainGuard is public in shared Primitives through a direct reference. User/UserId belong to Users owners; Exercises Domain no longer references central Domain or uses its IVT.
- Persistence: ExerciseEntryRepository, ExerciseEntryConfiguration, four scoped aliases. Shared DbContext, Tracking DbSet, migrations and snapshot stay central. Explicit model registration prevents circular central Infrastructure -> module Infrastructure edges.
- No owned provider, worker, scheduler, webhook or configuration discovered. Repository writes remain tracked operations; shared runtime commits.
- Tests: move three central Exercises application suites and ExerciseEntryInvariantTests. Split only ExerciseEntry methods from TrackingEntryInvariantTests; retain Hydration. TrackingAndMealPlanCoverageGapTests currently contains only MealPlan methods: do not alter it.
- Relational: PersistenceRepositoryCoverageIntegrationTests exercises actual repository via shared PostgreSQL fixture; retain cross-module fixture/suite centrally and add module Infrastructure reference/IVT.
- Other consumers: central Errors facade, HTTP controller/mappings, Dashboard fallback, TDEE, API/Initializer/JobManager composition, shared context and user data lifecycle, host/DI tests. HTTP/host/cross-module tests remain central.
- Invariants: calories finite 0..10000, one-decimal ToEven rounding, duration 1..1440, trimmed optional text, unspecified date -> UTC date and local -> UTC date, current-user access, user-scoped repository predicate, cancellation, no added saves or changed transaction semantics.

Wiki omissions verified against source: domain invariant and mixed Tracking tests, relational flow, the former internal DomainGuard/IVT seam (now removed), central Errors facade. No source changes yet.
