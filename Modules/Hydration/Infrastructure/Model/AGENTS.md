# Hydration Persistence Model Guidelines

Hydration EF configurations and the model-builder registration seam live here. Preserve tables, columns, indexes, conversions and the User FK/cascade relationship through `HasOne<User>().WithMany()`. The domain stores only UserId; neither side has a CLR navigation. Users Domain is a persistence-model dependency, not a Hydration Domain dependency.

Do not reference central Infrastructure/application projects or add a DbContext. Navigation metadata changes require snapshot and PostgreSQL validation; unchanged relational schema does not need an empty migration. See ADR 0029.
