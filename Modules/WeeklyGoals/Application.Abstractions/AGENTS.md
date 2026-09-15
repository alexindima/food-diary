# Weekly Goals Application Abstractions Guidelines

Repository ports and transaction seams live here. Depend on the WeeklyGoals Domain project for module-owned types; reference Users Domain.Contracts directly for `UserId`. Use canonical project/folder namespaces and do not reference EF Core, hosts, presentation, central Infrastructure, or application implementations.

All module projects and tests use `FoodDiary.Modules.WeeklyGoals.<Project>` identities and namespaces matching physical folders. Projects are siblings, including Application.Abstractions and PersistenceModel. Namespace changes preserve database schema, historical migration metadata, HTTP payloads and runtime behavior.
