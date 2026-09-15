# Shared audit infrastructure

Own audit adapters and explicit service registration. Consume the same scoped
SharedPersistenceDbContext so owner changes and shared records commit together.
Do not reference module implementations, the complete migration model or hosts.
Do not register persistence coordination implicitly. Preserve existing lifetimes,
SQL, cancellation, logging fields and transaction behavior.
