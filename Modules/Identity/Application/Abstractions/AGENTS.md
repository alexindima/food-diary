# Identity application contracts

Own authentication/provider/token/session/login-event contracts and email-template
administration/provider contracts. Authentication and legacy Admin email-template
namespaces are preserved; their declaring assembly now belongs to Identity.

Users owns credential state and exposes capabilities through Users Contracts.
Depend one-way on those contracts, Identity Domain, Users Domain.Contracts and
Results. Never depend on central Application.Abstractions or implementation/HTTP
assemblies. Keep token formats, claim names, payloads, cancellation and security
helper algorithms unchanged during relocation.

IAdminSsoCodeStore remains a shared central contract used by ordinary Identity
SSO and Admin impersonation; its in-memory/Redis implementations do not move.
Shared rendered-email transport/outbox contracts also remain central.
