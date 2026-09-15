# USDA Tests

Keep focused USDA application, domain and mocked provider suites here without duplicating donor coverage. Infrastructure.Tests owns provider mapping/cache/cancellation/options/registration tests. Shared DbContext, migrations, HTTP, host, Products and Meals scenarios remain with their current owners.

HealthArea value-object and calculation tests belong to the USDA Domain suite. Preserve moved cases; mixed Products/USDA invariant tests remain central.

All module projects and tests use `FoodDiary.Modules.Usda.<Project>` identities and namespaces matching physical folders. Projects are siblings, including Application.Abstractions and PersistenceModel. Namespace changes preserve database schema, historical migration metadata, HTTP payloads and runtime behavior.
