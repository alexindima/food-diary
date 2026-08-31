# Admin Domain guidelines

Rules for `Modules/Admin/Domain/`.

Own AdminImpersonationSession and its invariants. Keep scalar UserId dependencies one-way toward central Domain and preserve CLR namespace. No EF or application dependencies.
