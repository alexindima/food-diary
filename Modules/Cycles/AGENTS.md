# Cycles Logical Module Guidelines

## Boundary

- Own cycle profiles, factors, symptoms, bleeding entries, fertility signals, menstrual episodes, consent, predictions, use cases, persistence ports, EF configuration, and repository behavior.
- Use canonical FoodDiary.Modules.Cycles project names and project-relative namespaces; Application.Abstractions and PersistenceModel are sibling projects.
- Keep `User` in Users Domain and `UserId` in Users Domain.Contracts; the Cycles relationship is unidirectional from `CycleProfile`.
- Register application behavior through `AddCyclesApplication`; composition roots use Infrastructure's `AddCyclesModule` facade.
- Keep `FoodDiaryDbContext`, historical migrations, and the model snapshot in central Infrastructure.
- Keep HTTP transport in `Modules/Cycles/Presentation`; keep Dashboard/Export read contracts in Contracts and handlers in Application.

## Verification

- `dotnet test Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/FoodDiary.Modules.Cycles.Application.Tests.csproj`
- `dotnet test Modules/Cycles/tests/FoodDiary.Modules.Cycles.Domain.Tests/FoodDiary.Modules.Cycles.Domain.Tests.csproj`
- `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

Users owns the complete User aggregate, all credential/security partials, roles,
role audit and weight/waist goals under `Modules/Users/Domain`. `UserId` lives in
`Modules/Users/Domain.Contracts`, depending only on shared primitives. Consumers
reference the exact owner; shared guards and generic values belong to
`FoodDiary.Domain.Primitives`; module-specific values stay with their owner. Authentication flows/providers, combined UserRepository,
DbContext, migrations and snapshot retain their existing owners. CLR namespaces,
security behavior and EF/HTTP contracts are unchanged. See
`docs/ai/users-domain-extraction.md` for residual seams and verification evidence.

## Error ownership

Feature error factories belong to their existing owner contracts; call them directly.
The corresponding central Errors facades are retired. Preserve exact codes, messages,
kinds and parameter formatting. Reference the owner explicitly; this grants no foreign
repository or aggregate capability. See docs/ai/feature-error-retirement.md.

## Consumer boundary

Own the cycle read DTOs, GetCurrentCycleQuery consumed by Dashboard and Export. Keep handlers, repositories and mutation policy in Application. The eleven public cycle enums belong to dependency-free Cycles Domain.Contracts; do not expose aggregate instances through these contracts. See `Contracts/AGENTS.md` and ADR 0033.

Consumer Contracts must not reference Cycles Domain. Preserve enum values and wire fields.

CyclesDbContext is the fourth owned runtime context (ADR 0040), containing the profile and seven owned child types. Shared UoW, central migrations, composed reads and purge remain; no User aggregate enters the runtime model.

Read use cases are owner requests dispatched through ISender. Keep current-profile and nutrition orchestration in their query handlers; the internal nutrition calculator and reused prediction/revision algorithms retain their existing calculations. Export preserves user access, sensitive-export credential verification and CSV fields. Keep only ICycleWriteRepository and ICycleReadModelRepository; do not restore ICycleRepository, ICycleReadRepository or the unused CycleDayErrors. Domain invariants are tested in this module.
