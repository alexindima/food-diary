# Fasting Domain Guidelines

## Scope

Rules for `Modules/Fasting/Domain/`.

## Role

- Own Fasting aggregates, entities, enums, and strongly typed identifiers.
- Keep domain behavior independent from application, persistence, transport, and host concerns.

## Boundaries

- Reference Users Domain.Contracts for UserId and use shared Primitives for base types; validation helpers remain Fasting-owned.
- Do not reference Application, Contracts, Infrastructure, EF Core, or ASP.NET packages.
- Keep database mapping and repository behavior outside this project.

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.

Use the canonical project name as the namespace root and match folders. Projects are siblings. Public owner use cases are Contracts requests dispatched through ISender; keep outbound source ports and reusable algorithms separate. Preserve authorization, cancellation, wire shapes and persistence semantics.

FastingPlanSettings is closed to external derivation and has three sealed factory-created variants: IntermittentFastingSettings, ExtendedFastingSettings and CyclicFastingSettings. DailyFastingWindow validates a 24-hour paired window; FastingCycleDay represents the UTC calendar anchor/next phase, not an instant. Application starts use CreateWithSettings and cyclic scheduling uses ScheduleNextCyclicDay. Nullable stored mode fields and existing historical read fallbacks remain unchanged; primitive compatibility constructors preserve user/protocol/window/day/title/timestamp validation order.
