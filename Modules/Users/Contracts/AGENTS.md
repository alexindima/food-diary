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
