# Ai module guidelines

Own AiUsage/AiPromptTemplate and IDs with preserved CLR names and invariants. Depend only on Users Domain.Contracts for UserId; do not move User profile quota/consent state or Meals AI entities.

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.
