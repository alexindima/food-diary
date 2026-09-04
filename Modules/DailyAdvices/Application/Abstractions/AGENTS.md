# Daily Advices Application Abstractions Guidelines

- Own module-internal repository ports and persistence projections.
- Own DailyAdviceErrors; consumers call it directly using shared Results. The central Errors.DailyAdvice facade is retired; preserve codes/messages/kinds and locale semantics.
- Preserve legacy namespaces.
- Do not reference EF Core, hosts, presentation, or central Infrastructure.
