# Notifications Domain

- Own Notification, WebPushSubscription and their strongly typed IDs.
- Preserve legacy CLR namespaces and all aggregate invariants.
- Reference Users Domain for User, Users Domain.Contracts for UserId and shared Primitives for generic guards; never reference persistence or application.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and residual central Domain only for shared values. Preserve all existing relationships.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access. This domain has no central Domain dependency.
