# Admin Domain guidelines

Rules for `Modules/Admin/Domain/`.

Own AdminImpersonationSession and its invariants. Keep scalar UserId dependencies one-way toward Users Domain.Contracts and preserve CLR namespace. No EF or application dependencies.

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.
