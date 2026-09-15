# Shared authentication contracts

Own only technical authentication contracts that are deliberately shared across
bounded contexts and hosts. Identity owns ordinary authentication protocols;
Admin owns impersonation workflows. IAdminSsoCodeStore is the narrow shared
single-use-code storage seam between them.

IPasswordHasher and AuthenticationInputLimits are shared technical password contracts.
Identity owns the hashing implementation; Users consumes the contract directly.
Preserve the existing CLR namespaces and hashing/input-limit behavior.

JWT configuration shape and pure validation belong to Options/JwtOptions. Central composition binds the unchanged Jwt section; Identity issues tokens and the API validates them using this same shared type. Do not add provider or persistence dependencies here.
