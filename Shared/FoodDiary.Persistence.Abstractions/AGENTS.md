# Persistence abstractions

Infrastructure-only contracts for registering owner EF contexts with the scoped persistence coordinator. Depend only on EF Core and generic framework primitives. Do not add aggregate types, provider implementations, migrations, application use cases or independent save/transaction APIs. IModuleTransactionCoordinator coordinates the existing scoped unit of work; it does not create independent contexts or expose a separate save API. Application and Domain projects must not reference this assembly.

IModuleContextFactory preserves the coordinator's provider, shared connection, command interceptors and save ordering. The caller registers the returned context with scoped DI ownership. The existing central unit of work owns coordinated saving and transaction behavior.

ExecuteSerializableAsync preserves the catalog mutation policy: Serializable isolation and whole-attempt retries on relational providers, with the existing single-attempt behavior for nonrelational tests. It uses the same scoped unit of work, clean-entry check and failed-attempt reset. Do not place external provider calls inside retried operations.

The command ExecuteAsync overload always invokes the shared unit of work, including domain-event dispatch without tracked property changes. Its owner-supplied exception translator runs within each attempt after transaction disposal but before reset; preserve unknown exception instances and avoid external side effects. The existing generic/Serializable overloads retain conditional saving.

IModuleSessionCoordinator serializes one scoped callback with a separate PostgreSQL session lease and no surrounding database transaction. The callback runs once, persistence alone may retry, and successful results always invoke IUnitOfWork. Failed results/exceptions discard unsaved tracking; intentional intermediate saves remain durable. Preserve the existing Wearables null post-commit-queue behavior. Do not substitute the whole-attempt transaction coordinator.

IModuleScopeGuard checks live shared transaction and pending changes across registered module contexts before independent outbox work. It only rejects an unclean scope; it cannot save, reset, start a transaction or change post-commit actions. Keep this inspection separate from transaction execution and module context creation.

IIndependentModuleContextOptionsFactory copies the configured provider/core extensions for already-reviewed independent operations such as Ai quota and recognition jobs. It does not resolve a live context, open a connection, create a transaction or register a unit-of-work participant. Connection ownership follows the configured options: do not silently replace a configured connection with the live scoped connection or strip interceptors/retry settings. New independent writes still require explicit ownership and transaction review.

IModuleSessionLock acquires a separate database session lease for an owner-derived numeric key. Callers dispose the lease; shared transaction commit/rollback must not release it. It exposes no connection, saves or callback retries. Keep it distinct from the session coordinator, which also owns persistence and cleanup.
