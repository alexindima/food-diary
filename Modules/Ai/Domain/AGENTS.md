# Ai module guidelines

Own AiUsage/AiPromptTemplate and IDs with project-and-folder namespaces and unchanged invariants. Depend only on Users Domain.Contracts for UserId; do not move User profile quota/consent state or Meals AI entities.

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.

AiTokenUsage groups immutable nonnegative input/output/total counts with total >= input+output using widened addition; provider overhead is permitted. TryFromProviderCounts returns absent usage for malformed totals/overflow instead of introducing a persistence failure. Quota reconciliation uses this group and preserves existing estimated fallback, locks, reservation accounting and compensation.
