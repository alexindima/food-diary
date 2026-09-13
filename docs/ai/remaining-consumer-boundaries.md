# Remaining consumer boundary extraction

Identity.Contracts exposes existing email/template and login-event read APIs plus
an impersonation-only token capability. Identity.Application.Abstractions retains
repository, session, provider and generic JWT ports. Admin passes the existing
subject, roles, actor, reason and security version through an immutable request;
the singleton Identity adapter delegates to the same JWT overload. Admin keeps
authorization, target restrictions, audit, handoff and transaction behavior.

BodyMetrics.Contracts owns weight/waist read services and entry/summary DTOs.
Repositories remain in owner Abstractions. ProductErrors, RecipeErrors and
CycleErrors now live in existing owner Contracts with unchanged codes/messages.
Images.Service.Contracts also owns cleanup/ownership capabilities and the deletion
result. Implementations, object deletion and persistence behavior are unchanged.

The redundant Export-to-Identity and AI-to-Images broad references are removed.
Consumers directly reference the actual type owner; namespace compatibility does
not justify retaining an unused project reference. New assembly dependencies and
all three Docker restore/source inventories are updated for coordinated builds.

No routes, wire schemas, migrations or provider configuration change. Preserve
existing assembly-wide internal ports for host composition and owner tests. Shared
DbContext access and other consumer-owned abstractions remain separate concerns.
