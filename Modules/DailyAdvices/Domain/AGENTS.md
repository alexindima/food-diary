# Daily Advices Domain Guidelines

- Own `DailyAdvice` and `DailyAdviceId` while preserving legacy CLR namespaces and EF identity.
- Reference central Domain only for shared domain primitives.
- Do not reference Application, Infrastructure, EF Core, or transport.
