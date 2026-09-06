# Cycles Logical Module Guidelines

## Boundary

- Own cycle profiles, factors, symptoms, bleeding entries, fertility signals, menstrual episodes, consent, predictions, use cases, persistence ports, EF configuration, and repository behavior.
- Preserve legacy CLR namespaces and the `FoodDiary.Application.Cycles` application assembly name.
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

Own the cycle read DTOs, ICycleReadService and GetCurrentCycleQuery consumed by Dashboard and Export. Keep handlers, repositories and mutation policy in Application. Existing cycle enums remain in Cycles Domain; do not expose aggregate instances through these contracts. See `Contracts/AGENTS.md` and ADR 0033.
