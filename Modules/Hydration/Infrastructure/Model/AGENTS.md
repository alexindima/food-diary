# Hydration Persistence Model Guidelines

Hydration EF configurations and the model-builder registration seam live here. Preserve tables, columns, indexes, conversions, the forward `HydrationEntry.User` navigation, and the unidirectional User FK/cascade relationship. Do not restore an inverse User collection, reference central Infrastructure/application projects, or add a DbContext.
