# Fasting Contracts Guidelines

## Scope

Rules for `Modules/Fasting/Contracts/`.

## Role

- Own stable read projections and owner query/command contracts consumed outside the Fasting implementation assembly.
- Remain independent from Fasting implementation, persistence, HTTP transport, EF Core, and host configuration.

## Boundaries

- Do not add repository interfaces, aggregates, command handlers, validators, or concrete services.
- Do not reference `FoodDiary.Modules.Fasting` or `FoodDiary.Infrastructure`.
- Align public namespaces with `FoodDiary.Modules.Fasting.Contracts.*` and keep those contracts backward-compatible after extraction.
- Async contracts accept `CancellationToken`.

Telemetry owns the existing GetFastingTelemetrySummaryQuery and two immutable summary models consumed by Admin Presentation. Their legacy CLR namespaces are preserved for compatibility; handlers, validators and persistence remain in their existing projects.

Use the canonical project name as the namespace root and match folders. Projects are siblings. Public owner use cases are Contracts requests dispatched through ISender; keep outbound source ports and reusable algorithms separate. Preserve authorization, cancellation, wire shapes and persistence semantics.
