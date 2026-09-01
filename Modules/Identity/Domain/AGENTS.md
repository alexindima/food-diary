# Identity domain

Own `EmailTemplate`, `UserRefreshTokenSession`, and `UserLoginEvent` while preserving their legacy `FoodDiary.Domain` CLR namespaces. The project depends one-way on central `FoodDiary.Domain` for `Entity`, `LanguageCode`, and the compatibility `UserId` type.

Keep `User`, `Role`, `UserRole`, `UserRoleAuditEvent`, credentials, password/reset/email-confirmation state, and the rest of the `User` aggregate central.
