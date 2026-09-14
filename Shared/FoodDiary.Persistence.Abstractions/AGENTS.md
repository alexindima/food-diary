# Persistence abstractions

Infrastructure-only contracts for registering owner EF contexts with the scoped persistence coordinator. Depend only on EF Core and generic framework primitives. Do not add aggregate types, provider implementations, migrations, application use cases or independent save/transaction APIs. IModuleTransactionCoordinator coordinates the existing scoped unit of work; it does not create independent contexts or expose a separate save API. Application and Domain projects must not reference this assembly.

IModuleContextFactory preserves the coordinator's provider, shared connection, command interceptors and save ordering. The caller registers the returned context with scoped DI ownership. The existing central unit of work owns coordinated saving and transaction behavior.
