# Daily Advices Application Abstractions Guidelines

- Own module-internal repository ports and persistence projections.
- Own DailyAdviceErrors; use shared Results directly and keep central Errors as a delegating compatibility facade.
- Preserve legacy namespaces.
- Do not reference EF Core, hosts, presentation, or central Infrastructure.
