# Ai module guidelines

Own AiUsage/AiPromptTemplate and IDs with preserved CLR names and invariants. Depend only on Users Domain.Contracts for UserId; do not move User profile quota/consent state or Meals AI entities.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and residual central Domain only for shared values/guards. Preserve all existing relationships.
