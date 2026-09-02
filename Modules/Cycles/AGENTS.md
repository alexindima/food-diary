# Cycles Logical Module Guidelines

## Boundary

- Own cycle profiles, factors, symptoms, bleeding entries, fertility signals, menstrual episodes, consent, predictions, use cases, persistence ports, EF configuration, and repository behavior.
- Preserve legacy CLR namespaces and the `FoodDiary.Application.Cycles` application assembly name.
- Keep `User` in Users Domain and `UserId` in Users Domain.Contracts; the Cycles relationship is unidirectional from `CycleProfile`.
- Register application behavior through `AddCyclesApplication`; composition roots use Infrastructure's `AddCyclesModule` facade.
- Keep `FoodDiaryDbContext`, historical migrations, and the model snapshot in central Infrastructure.
- Keep HTTP transport in central Presentation and do not introduce an unproven Contracts project.

## Verification

- `dotnet test Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/FoodDiary.Modules.Cycles.Application.Tests.csproj`
- `dotnet test Modules/Cycles/tests/FoodDiary.Modules.Cycles.Domain.Tests/FoodDiary.Modules.Cycles.Domain.Tests.csproj`
- `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

Users owns the complete User aggregate, all credential/security partials, roles,
role audit and weight/waist goals under `Modules/Users/Domain`. `UserId` lives in
`Modules/Users/Domain.Contracts`, depending only on shared primitives. Consumers
reference the exact owner; central Domain retains shared guards and values without
an aggregate re-export. Authentication flows/providers, combined UserRepository,
DbContext, migrations and snapshot retain their existing owners. CLR namespaces,
security behavior and EF/HTTP contracts are unchanged. See
`docs/ai/users-domain-extraction.md` for residual seams and verification evidence.
