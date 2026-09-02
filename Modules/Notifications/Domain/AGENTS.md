# Notifications Domain

- Own Notification, WebPushSubscription and their strongly typed IDs.
- Preserve legacy CLR namespaces and all aggregate invariants.
- Reference Users Domain for User, Users Domain.Contracts for UserId and central Domain for shared guards; never reference persistence or application.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and residual central Domain only for shared values/guards. Preserve all existing relationships.
