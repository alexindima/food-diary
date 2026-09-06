# Notifications Domain

- Own Notification, WebPushSubscription and their strongly typed IDs.
- Preserve legacy CLR namespaces and all aggregate invariants.
- Reference Users Domain.Contracts for UserId and shared Primitives for generic guards; never reference persistence or application.

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access. This domain has no central Domain dependency.
