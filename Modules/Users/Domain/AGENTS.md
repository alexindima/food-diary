# Users Domain

Own the complete User aggregate, including every credential/security partial,
roles, role audit, weight/waist goals, lifecycle events and User-specific state and
value objects. Preserve legacy CLR namespaces and all invariants. Keep authentication
flows and providers in Identity and their established adapters.

Own DesiredWeightKg and DesiredWaistCm with their unchanged limits and validation.
Reference Users Domain.Contracts for UserId, ActivityLevel and LanguageCode,
Primitives for generic guards and EmailAddress, and Images Contracts for ImageAssetId.
Keep the dashboard-layout JSON limit owner-local at 65536.
Do not restore foreign inverse navigations or reference application/persistence.
Keep User goal collections and UserRole/Role relationships unchanged.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access.
