# Reusable presentation contracts

Own immutable wire DTOs with preserved JSON shapes and canonical project/folder namespaces. Reference only
other Presentation.Contracts required by composite responses. No mappings, methods,
application models, controllers, framework packages, providers or persistence.

All module projects and tests use `FoodDiary.Modules.Users.<Project>` identities and namespaces matching physical folders. Projects are siblings, including Application.Abstractions and PersistenceModel. Namespace changes preserve database schema, historical migration metadata, HTTP payloads and runtime behavior.
