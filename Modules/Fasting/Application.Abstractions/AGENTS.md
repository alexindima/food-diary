# Fasting Application Abstractions Guidelines

## Scope

Rules for `Modules/Fasting/Application.Abstractions/`.

## Role

- Own internal repository ports, persistence projections, and application read abstractions for Fasting.
- Own FastingErrors; callers use this factory directly. Preserve codes/messages/kinds; do not restore the retired central Errors.Fasting facade.

## Boundaries

- Depend only on Fasting Domain and shared result primitives.
- Do not reference Infrastructure, EF Core, hosts, or presentation projects.
- Keep stable cross-module DTOs and services in `Modules/Fasting/Contracts` instead.

Use the canonical project name as the namespace root and match folders. Projects are siblings. Public owner use cases are Contracts requests dispatched through ISender; keep outbound source ports and reusable algorithms separate. Preserve authorization, cancellation, wire shapes and persistence semantics.
