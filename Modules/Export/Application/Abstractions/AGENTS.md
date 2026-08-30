# Export Application Abstractions Guidelines

## Scope

Rules for `Modules/Export/Application/Abstractions/`.

## Boundary

- Own export-specific file generation, report text, input-limit, and diary composition contracts used by adapters and presentation.
- Keep namespaces stable under `FoodDiary.Application.Abstractions.Export`.
- Do not depend on Application implementation, infrastructure, resources, presentation, hosts, provider SDKs, or ASP.NET types.
- Async contracts accept and propagate `CancellationToken`.
