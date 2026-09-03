# Users logical module

Users owns the complete User aggregate, all credential/security partials, roles,
role audit and weight/waist goals under `Modules/Users/Domain`. `UserId` lives in
`Modules/Users/Domain.Contracts`, depending only on shared primitives. Consumers
reference the exact owner; shared guards and generic values belong to
`FoodDiary.Domain.Primitives`; module-specific values stay with their owner. Authentication flows/providers, combined UserRepository,
DbContext, migrations and snapshot retain their existing owners. CLR namespaces,
security behavior and EF/HTTP contracts are unchanged. See
`docs/ai/users-domain-extraction.md` for residual seams and verification evidence.

Preserve the FoodDiary.Application.Users assembly identity. Keep shared Users application contracts central and compose AddUsersModule from the existing hosts.

Users Infrastructure owns the independent access-token security-state reader.
The central port is unchanged; API bearer validation consumes it without receiving
the User aggregate. The mixed UserRepository retains its other responsibilities.
See `docs/ai/users-security-reader-ownership.md` for the port inventory and next steps.

Administrative user read projections and both legacy/model repository ports now
belong to the same Users Infrastructure adapter. The central repository retains
tracked lookup, Google identity and writes. API/Admin policies and the existing
UserAdministrationReadService contract are unchanged; see
`docs/ai/users-administration-reader-ownership.md`.
