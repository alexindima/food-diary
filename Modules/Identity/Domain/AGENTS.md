# Identity domain

Own EmailTemplate, UserRefreshTokenSession and UserLoginEvent with stable CLR namespaces. Reference Users Domain.Contracts for LanguageCode and UserId; shared primitives are available through that contract dependency. The complete User aggregate, including credentials and role state, belongs to Users Domain; authentication workflows stay Identity-owned.
