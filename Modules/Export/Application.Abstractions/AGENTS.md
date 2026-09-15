# Export Application Abstractions Guidelines

## Scope

Rules for `Modules/Export/Application.Abstractions/`.

## Boundary

- Own export-specific file generation, report text, input-limit, and diary composition contracts used by adapters and presentation.
- Do not depend on Application implementation, infrastructure, resources, presentation, hosts, provider SDKs, or ASP.NET types.
- Async contracts accept and propagate `CancellationToken`.

Use the canonical project name as the namespace root and match folders. Projects are siblings. Public owner use cases are Contracts requests dispatched through ISender; keep outbound source ports and reusable algorithms separate. Preserve authorization, cancellation, wire shapes and persistence semantics.
