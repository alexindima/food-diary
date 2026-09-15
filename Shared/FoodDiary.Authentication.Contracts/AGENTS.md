# Shared authentication contracts

Own only technical authentication contracts that are deliberately shared across
bounded contexts and hosts. Identity owns ordinary authentication protocols;
Admin owns impersonation workflows. IAdminSsoCodeStore is the narrow shared
single-use-code storage seam between them.

IPasswordHasher and AuthenticationInputLimits are shared technical password and opaque-token contracts.
Identity owns the hashing implementation; Users consumes the contract directly.
Preserve the existing CLR namespaces and hashing/input-limit behavior.

AuthenticationErrors owns only InvalidCredentials and InvalidToken. Account/link
failures belong to Users.Contracts and impersonation failures to Admin.Contracts.
Protocol-specific input limits belong to Identity.Contracts/Authentication.

JWT configuration shape and pure validation belong to Options/JwtOptions. Shared/FoodDiary.Authentication.Infrastructure binds the unchanged Jwt section; Identity issues tokens and the API validates them using this same shared type. Do not add provider or persistence dependencies here.

ADR 0044: hosts explicitly compose AddSharedAuthentication and AddIdentityEmailOptions. Identity owns email link option binding; shared authentication owns JWT binding and fallback SSO storage.
