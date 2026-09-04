# Users logical module

Users owns the complete User aggregate, all credential/security partials, roles,
role audit and weight/waist goals under `Modules/Users/Domain`. `UserId` lives in
`Modules/Users/Domain.Contracts`, depending only on shared primitives. Consumers
reference the exact owner; shared guards and generic values belong to
`FoodDiary.Domain.Primitives`; module-specific values stay with their owner. Users
Infrastructure owns UserRepository; authentication flows/providers, shared DbContext,
migrations and snapshot retain their existing owners. CLR namespaces,
security behavior and EF/HTTP contracts are unchanged. See
`docs/ai/users-domain-extraction.md` for residual seams and verification evidence.

Preserve the FoodDiary.Application.Users assembly identity. Keep semantic capabilities/models in Users Contracts and the seven aggregate/repository ports in Application/Abstractions; retain central CurrentUserAccessResolver and compatibility references and compose AddUsersModule from the existing hosts.

Users Infrastructure owns the independent access-token security-state reader.
The port signature is unchanged and now belongs to Users Contracts; API bearer validation consumes it without receiving
the User aggregate. The tracked UserRepository remains a separate Users adapter.
See `docs/ai/users-security-reader-ownership.md` for the port inventory and next steps.

Administrative user read projections and both legacy/model repository ports now
belong to the same Users Infrastructure adapter. The separate Users repository retains
tracked lookup, Google identity and writes. API/Admin policies and the existing
UserAdministrationReadService contract are unchanged; see
`docs/ai/users-administration-reader-ownership.md`.

UserRepository and all four repository/lookup/write/Google aliases are registered
by AddUsersPersistence, sharing one scoped instance and DbContext. Identity uses
Users' semantic capabilities, not these aggregate ports. Preserve tracked identity,
query filters, role/goal loading and caller-owned SaveChanges/transactions. See
`docs/ai/users-repository-ownership.md`.
