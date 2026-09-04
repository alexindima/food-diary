# Dietologist Application Module Guidelines

## Scope

Rules for `Modules/Dietologist/Application/`.

## Boundaries

- This project owns Dietologist application use cases, policies, mappings, models, and services.
- Own DietologistRequiredIdParser and DietologistEnumValueParser under Common/Validation; preserve their namespaces, parsing/error behavior and focused tests.
- Depend on Dietologist Domain/Application.Abstractions, Users Domain/Domain.Contracts, shared application abstractions and Mediator, as declared in the project reference matrix.
- Reference Users Domain.Contracts directly for ActivityLevel already exposed by profile projections; this enum dependency does not broaden relationship permissions or access to user data.
- Interact with Users, Dashboard, Notifications and Audit through their established application contracts; use module `Application/Abstractions` for Dietologist persistence ports.
- Do not reference `FoodDiary.Application`, infrastructure, presentation, hosts, or provider SDKs.
- Register module-owned handlers, validators, and services through `AddDietologistApplication`; hosts use Infrastructure's `AddDietologistModule` facade.

## Commands

- Build: `dotnet build Modules/Dietologist/Application/FoodDiary.Modules.Dietologist.Application.csproj`
- Tests: `dotnet test Modules/Dietologist/tests/FoodDiary.Modules.Dietologist.Application.Tests/FoodDiary.Modules.Dietologist.Application.Tests.csproj`
- Guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
