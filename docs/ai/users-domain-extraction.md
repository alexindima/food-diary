# Users Domain extraction

Source baseline: `d3a1e7c65d1bad7ebff58dd97167434bbcd7f7cf`.

| Responsibility | Physical owner |
| --- | --- |
| User and all seven partial files, Role, UserRole, UserRoleAuditEvent, WeightGoal, WaistGoal | Modules/Users/Domain |
| User-prefixed states/updates, lifecycle events, RoleId/WeightGoalId/WaistGoalId, goal statuses and role audit enum/role names | Modules/Users/Domain |
| GenderCode, ProfileWeightKg, ProfileHeightCm, ThemeCode, UiStyleCode | Modules/Users/Domain; these encode User profile rules |
| UserId and ActivityLevel | Modules/Users/Domain.Contracts; only shared IEntityId primitives dependency |
| User/role/audit and both goal EF mappings | Modules/Users/Infrastructure/Model, selected by the existing ApplyUsersPersistenceModel registration |
| Constants, EmailAddress, LanguageCode, DesiredWeightKg/DesiredWaistCm, shared enums and food-health value objects | Residual FoodDiary.Domain |
| Combined Users/Identity UserRepository, DbContext/DbSets, historical migrations and snapshot | Central Infrastructure, unchanged |
| Authentication use cases, sessions/login events, tokens and providers | Existing Identity and integration owners, unchanged |
| Focused aggregate, state, role, goal, event and identifier tests | Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests |

EmailAddress validates Dietologist invitations as well as Users and Identity input.
LanguageCode also belongs to content localization in EmailTemplate and DailyAdvice.
DesiredWeightKg and DesiredWaistCm validate BodyMetrics entries, so their shared
validation semantics remain central. ActivityLevel belongs to Users Domain.Contracts
and is consumed through existing profile/read contracts. Its FoodDiary.Domain.Enums
namespace, names and numeric values remain unchanged, as do EF string conversion
and TDEE multipliers. Refer to the current scoped Domain guides for other enum
owners. No entity definition remains in central Domain. Its obsolete Product,
Recipe, Image and USDA project edges are removed after their final consumers move.

User-specific methods and persisted properties move without body changes. This
includes security-version increments, deletion/restore guards, token lifetime,
UTC normalization, role replacement and premium/trial rules. Existing goal
collections and role bidirectional relationships are retained. No previously
removed inverse collection is reintroduced. Goal configurations move unchanged;
the same shared DbContext invokes the existing Users model registration.

Consumers require a coordinated rebuild because assembly ownership changes while
CLR namespaces remain stable. The three Docker hosts include the two new projects.
Rollback is a complete rebuild of the previous revision; there is no data migration,
configuration change, new provider, background job or deployment in this task.

Verification and source hash evidence are retained separately under
`.artifacts/users-domain-evidence`; the governed workspace is
`.artifacts/llm-wiki/tasks/users-domain-extraction`. Execution results must be read
from the latest test ledger, not inferred from this inventory or prior extractions.
The adaptive start inferred stale Application/background-job acceptance; native
acceptance-init and delivery-replan replace that inference with Domain criteria.

Generic DomainGuard now belongs to shared Primitives through a direct public API reference; central Domain has no friend grants. See domain-guard-extraction.md.
