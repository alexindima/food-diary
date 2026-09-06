# Shared authentication contracts

Own only technical authentication contracts that are deliberately shared across
bounded contexts and hosts. Identity owns ordinary authentication protocols;
Admin owns impersonation workflows. IAdminSsoCodeStore is the narrow shared
single-use-code storage seam between them.

IPasswordHasher and AuthenticationInputLimits are shared technical password contracts.
Identity owns the hashing implementation; Users consumes the contract directly.
Preserve the existing CLR namespaces and hashing/input-limit behavior.
