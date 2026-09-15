# USDA Domain

Own USDA reference-data entities and HealthAreaScore, HealthAreaGrade and HealthAreaScores. Use canonical project/folder namespaces and the exact nutrient-ID-based calculation, sodium penalty, limits and grade rules. Reference shared Domain.Primitives for generic guards; never add a reverse dependency on Products or Users. Products Domain may reference this owner for nutritional value calculations; foreign aggregate links remain scalar.

All module projects and tests use `FoodDiary.Modules.Usda.<Project>` identities and namespaces matching physical folders. Projects are siblings, including Application.Abstractions and PersistenceModel. Namespace changes preserve database schema, historical migration metadata, HTTP payloads and runtime behavior.
