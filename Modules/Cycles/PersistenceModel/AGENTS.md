# Cycles Persistence Model Guidelines

- Own Cycles EF configurations and model-builder registration.
- Preserve tables, columns, indexes, conversions, cascades, and CLR entity identity.
- Depend on module Domain, Domain.Contracts for cycle enums, and EF Core; keep repositories and migrations outside.

- Keep UserId conversions local through Users.Domain.Contracts. User Cascade relationships are composed by CyclesCrossModuleRelationships in central Infrastructure; this model must not reference foreign Domain assemblies.
