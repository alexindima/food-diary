# Cycles Logical Module Guidelines

## Boundary

- Own cycle profiles, factors, symptoms, bleeding entries, fertility signals, menstrual episodes, consent, predictions, use cases, persistence ports, EF configuration, and repository behavior.
- Preserve legacy CLR namespaces and the `FoodDiary.Application.Cycles` application assembly name.
- Keep shared `User` and `UserId` as central Domain compatibility types; the Cycles relationship is unidirectional from `CycleProfile`.
- Register application behavior through `AddCyclesApplication`; composition roots use Infrastructure's `AddCyclesModule` facade.
- Keep `FoodDiaryDbContext`, historical migrations, and the model snapshot in central Infrastructure.
- Keep HTTP transport in central Presentation and do not introduce an unproven Contracts project.

## Verification

- `dotnet test Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/FoodDiary.Modules.Cycles.Application.Tests.csproj`
- `dotnet test Modules/Cycles/tests/FoodDiary.Modules.Cycles.Domain.Tests/FoodDiary.Modules.Cycles.Domain.Tests.csproj`
- `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
