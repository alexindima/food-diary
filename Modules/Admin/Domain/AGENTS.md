# Admin Domain guidelines

Rules for `Modules/Admin/Domain/`.

Own AdminImpersonationSession and its invariants. Keep scalar UserId dependencies one-way toward Users Domain.Contracts and preserve CLR namespace. No EF or application dependencies.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and the exact module or Primitives owner for shared values/guards. Preserve all existing relationships.
