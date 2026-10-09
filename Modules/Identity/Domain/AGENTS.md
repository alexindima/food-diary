# Identity domain

Own EmailTemplate, UserRefreshTokenSession, RefreshTokenSessionId and UserLoginEvent with stable CLR namespaces. Reference Users Domain.Contracts for LanguageCode and UserId and shared Domain.Primitives directly for entities and the session ID protocol. Keep refresh-session GUID wire/storage representations in adapters without implicit conversions on RefreshTokenSessionId. The complete User aggregate, including credentials and role state, belongs to Users Domain; authentication workflows stay Identity-owned.

Use canonical FoodDiary.Modules.Identity project identities and folder namespaces, including tests. Projects are siblings. Preserve historical migration metadata and relational schema.
