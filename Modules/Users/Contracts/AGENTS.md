# Users consumer contracts

Own semantic Users capabilities, profile/admin/authentication projection models,
account-status filtering and UserErrors. Preserve legacy namespaces, signatures,
nullability, error values and cancellation/default parameters.

Do not expose User/Role aggregates or repository interfaces here. The existing
UserCalorieSchedule and UserPreferenceUpdate values currently require a Users
Domain reference; this is a value-type compatibility seam, not permission to
acquire aggregates. Users Domain.Contracts supplies UserId.

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
