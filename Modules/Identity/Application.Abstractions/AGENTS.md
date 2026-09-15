# Identity application contracts

Public email/template and login-event read capabilities now belong to
Identity.Contracts. Keep repository, session, generic JWT and provider ports here;
foreign business modules must consume the narrow Contracts assembly.

Own internal authentication/provider/token/session/login-event and email-template
repository ports. Use FoodDiary.Modules.Identity.Application.Abstractions namespaces matching folders. Consumer template administration
returns snapshots through Identity.Contracts; EmailTemplate aggregates remain
behind owner repository ports and implementations.

Users owns credential state and exposes capabilities through Users Contracts.
Depend one-way on those contracts, Identity Domain, Users Domain.Contracts and
Results. Never depend on central Application.Abstractions or implementation/HTTP
assemblies. Keep token formats, claim names, payloads, cancellation and security
helper algorithms unchanged during relocation.

IAdminSsoCodeStore remains a shared central contract used by ordinary Identity
SSO and Admin impersonation; its in-memory/Redis implementations do not move.
Shared rendered-email transport/outbox contracts also remain central.

Password hashing contracts live in Shared/FoodDiary.Authentication.Contracts.
Identity implements Users-owned IUserSessionRevocationService through its scoped
refresh-token repository; Users does not consume Identity repository ports.

Use canonical FoodDiary.Modules.Identity project identities and folder namespaces, including tests. Projects are siblings. Preserve historical migration metadata and relational schema.
