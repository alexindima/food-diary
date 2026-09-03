# Users infrastructure

Own separable Users persistence adapters and complete module DI. Depend on the
central Infrastructure project only for the shared DbContext compatibility seam.
Do not absorb Identity repositories or provider services.

Own `UserAccessTokenSecurityReader`, scoped through `AddUsersPersistence`. Its
unchanged no-tracking query reads persisted active/deleted/security-version state;
API/Identity still owns token validation/issuance. Do not substitute an already
tracked User or cache state. The remaining central UserRepository lookup, Google,
Admin projection and write aliases still share one instance and scoped DbContext.
