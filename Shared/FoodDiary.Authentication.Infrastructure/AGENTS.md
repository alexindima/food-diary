# Shared authentication infrastructure

Own JWT configuration binding/validation and the fallback in-memory SSO code store.
Reference only Authentication.Contracts; never reference module implementations,
the full persistence model, or hosts. Identity owns token issuance and ordinary SSO
workflows; Admin owns impersonation; the API owns its Redis override.

Preserve singleton storage, supplied TimeProvider, cancellation, atomic one-time
consumption and exact expiry. AddSharedAuthentication preserves a host-registered
store. Keep JWT keys, validation messages and startup validation unchanged.
Focused tests belong to Shared/tests/FoodDiary.Authentication.Infrastructure.Tests.
