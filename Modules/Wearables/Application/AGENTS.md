# Wearables Application Module Guidelines

## Scope

Rules for `Modules/Wearables/Application/`.

## Role

- Own wearable connection, OAuth, synchronization, and daily-summary use cases.
- Depend on other business areas through their narrow owner Contracts projects.

## Boundaries

- Do not reference the core `FoodDiary.Application` project.
- Register handlers through `AddWearablesApplication`; complete module registration belongs to Infrastructure.
- Keep provider clients, persistence implementations, token protection, HTTP transport, and host configuration outside this project.

All module projects and tests use `FoodDiary.Modules.Wearables.<Project>` identities and namespaces matching physical folders. Projects are siblings, including Application.Abstractions and PersistenceModel. Namespace changes preserve database schema, historical migration metadata, HTTP payloads and runtime behavior.

Connect, disconnect and sync share one serialization key per user/provider connection, irrespective of date. Refresh returns Result: only an explicit authentication rejection deactivates the connection; temporary provider/transport failures preserve tokens and active state.
