# Weekly Check-In Application Test Guidelines

Keep WeeklyCheckIn-owned query, calculation, and application-service tests here. Use canonical test project namespaces, depend on production collaborators through stable contracts in new coverage, and keep HTTP, host, shared persistence, and cross-module scenarios in their central test projects.

Run `dotnet test FoodDiary.Modules.WeeklyCheckIn.Application.Tests.csproj` from this directory.

All module projects and tests use `FoodDiary.Modules.WeeklyCheckIn.<Project>` identities and namespaces matching physical folders. Projects are siblings, including Application.Abstractions and PersistenceModel. Namespace changes preserve database schema, historical migration metadata, HTTP payloads and runtime behavior.
