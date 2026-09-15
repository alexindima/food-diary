# Users consumer contracts

Own semantic Users capabilities, profile/admin/authentication projection models,
account-status filtering and UserErrors. Preserve legacy namespaces, signatures,
nullability, error values and cancellation/default parameters.

Users/Common/UserAuthenticationErrors owns account-state and identity-link failures.
Preserve Authentication-prefixed wire codes, which differ from existing UserErrors.
Shared AuthenticationErrors supplies generic credential/token failures. Never depend
on Identity or Admin to obtain error factories.

Do not expose User/Role aggregates or repository interfaces here. The existing
UserCalorieSchedule, UserPreferenceUpdate and UserId belong to Users
Domain.Contracts. Do not reference the aggregate-bearing Users Domain assembly.

Do not reference the retired central Application.Abstractions, Identity contracts, application
implementations, Infrastructure, HTTP or provider SDKs. Identity contracts may
depend on these Users capabilities, never the reverse. CurrentUserAccessResolver
and UserIdParser belong here because they express the shared Users access boundary;
they may use generic shared application contracts and Results.

IUserSessionRevocationService and IUserProfileImageService are consumer-owned semantic capabilities implemented by Identity and Images. Expose revocation, URL resolution and cleanup requests without foreign aggregates or repositories.

IUserFastingReminderReadService and IUserCommentAuthorReadService expose batch
read-only dictionaries keyed by UserId, containing only reminder hours or author
names. Missing users are omitted. These preserve existing related-data reads for
all account states; they do not grant authentication/access or filter inactive
and soft-deleted accounts. No IQueryable or aggregate escapes these contracts.

Administration reads/mutations and billing access/profile/trial/Premium operations use public requests. Their handlers remain in Users Application. Mutations participate in the caller unit of work. Billing dispatches CheckUserAccessQuery; the existing narrow ICurrentUserAccessService capability remains valid for other callers.

Public request slices live under Users/Commands and Users/Queries alongside the legacy Users/Common and Users/Models folders. Their namespaces explicitly include Users and follow this project's existing FoodDiary.Application.Abstractions root. Do not restore unowned Abstractions.Commands/Queries namespaces. All consumers rebuild together for this namespace change.
