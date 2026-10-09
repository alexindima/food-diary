# Users Domain

Account principal, UserPreferences and UserNutritionProfile have independent
persistence versions and audit timestamps (ADR 0052). User methods remain the
validation facade. Preference/nutrition-only mutations touch their owner record;
credential/lifecycle changes touch the account and preserve security invalidation.

Own the complete User aggregate, including every credential/security partial,
roles, role audit, weight/waist goals, lifecycle events and User-specific state and
value objects. Use canonical project/folder namespaces and all invariants. Keep authentication
flows and providers in Identity and their established adapters.

Reusable UserCalorieSchedule and UserPreferenceUpdate live in Users.Domain.Contracts;
consume those exact types rather than duplicating their value semantics here.

Own DesiredWeightKg and DesiredWaistCm with their unchanged limits and validation.
Reference Users Domain.Contracts for UserId, ActivityLevel and LanguageCode,
Primitives for generic guards and EmailAddress, and Images Contracts for ImageAssetId.
Keep the dashboard-layout JSON limit owner-local at 65536.
Do not restore foreign inverse navigations or reference application/persistence.
Keep User goal collections and UserRole/Role relationships unchanged.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access.

`UserPersonalInfoUpdate` consumes owner-local `ProfileWeightKg` and `ProfileHeightCm` as distinct nullable immutable references. Null means an omitted measurement; neither positive value may have an invalid zero struct default or implicit numeric conversion. The scalar personal-info overload is a compatibility adapter through the same core validation/audit path. Preserve birth-date specified clearing, numeric limits, profile/account partition timestamps and BMR/TDEE calculations; wire/state/persistence numbers remain scalar.

RoleNames belongs to Users Domain.Contracts; role entities and membership invariants remain here.

All module projects and tests use `FoodDiary.Modules.Users.<Project>` identities and namespaces matching physical folders. Projects are siblings, including Application.Abstractions and PersistenceModel. Namespace changes preserve database schema, historical migration metadata, HTTP payloads and runtime behavior.

Goal starts use distinct DesiredWeightKg/MeasuredWeightKg or DesiredWaistCm/MeasuredWaistCm inputs. Typed and scalar starts share the attachment/replacement mutation core. Cancellation needs an ending measurement only when an active goal exists; a missing active goal still permits clearing the desired value without validating an unused timestamp or measurement. Preserve goal history, replacement chronology, profile-only audits and scalar compatibility error ordering.

UserPersonalInfoChanges carries birth-date Unchanged/Set/Clear state. BmrCalculationInput groups profile kilograms/centimeters/date/gender: FromMeasurements uses existing validated owner values, while FromStoredProfile preserves the calculator legacy/missing-data policy and exact Mifflin-St Jeor rounding. Raw personal-info and calculation adapters remain compatibility APIs.
