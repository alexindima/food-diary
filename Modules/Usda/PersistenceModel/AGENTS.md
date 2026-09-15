# USDA Persistence Model

Own USDA EF configurations and explicit model-builder registration. Preserve tables, keys, columns, relationships, and entity identity.

All module projects and tests use `FoodDiary.Modules.Usda.<Project>` identities and namespaces matching physical folders. Projects are siblings, including Application.Abstractions and PersistenceModel. Namespace changes preserve database schema, historical migration metadata, HTTP payloads and runtime behavior.
