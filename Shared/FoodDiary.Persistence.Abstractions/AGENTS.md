# Persistence abstractions

Infrastructure-only contracts for registering owner EF contexts with the scoped persistence coordinator. Depend only on EF Core and generic framework primitives. Do not add aggregate types, provider implementations, migrations, application use cases or independent save/transaction APIs. IModuleTransactionCoordinator coordinates the existing scoped unit of work; it does not create independent contexts or expose a separate save API. Application and Domain projects must not reference this assembly.

IModuleContextFactory preserves the coordinator's provider, shared connection, command interceptors and save ordering. The caller registers the returned context with scoped DI ownership. The existing central unit of work owns coordinated saving and transaction behavior.

ExecuteSerializableAsync preserves the catalog mutation policy: Serializable isolation and whole-attempt retries on relational providers, with the existing single-attempt behavior for nonrelational tests. It uses the same scoped unit of work, clean-entry check and failed-attempt reset. Do not place external provider calls inside retried operations.

The command ExecuteAsync overload always invokes the shared unit of work, including domain-event dispatch without tracked property changes. Its owner-supplied exception translator runs within each attempt after transaction disposal but before reset; preserve unknown exception instances and avoid external side effects. The existing generic/Serializable overloads retain conditional saving.
