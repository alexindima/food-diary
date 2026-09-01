# Users logical module

Users owns profile, preferences, goals, lifecycle, role membership, billing-profile
and notification-profile use cases; its application-facing ports and models; its
role/profile/cleanup adapters; and the `User`, `Role`, `UserRole`, and
`UserRoleAuditEvent` EF mappings.

Preserve the legacy `FoodDiary.Application.Users` assembly and CLR namespaces.
Keep the shared `User` aggregate, role CLR types, `FoodDiaryDbContext`, migrations,
snapshot, and the combined `UserRepository` central as a compatibility seam.
Credentials, password/reset/email-confirmation/security state, authentication
ports/services, external identities, token issuance, refresh-token/login-event
persistence, and email templates remain Identity/central responsibilities.

