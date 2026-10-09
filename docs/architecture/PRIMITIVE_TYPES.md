# Primitive types and migration priorities

FoodDiary benefits from domain types where values carry identity, units, temporal
meaning or a combined invariant. Its existing typed IDs, enums, nutrition value
objects and domain guards already protect many of these boundaries. The highest
review priorities are quantity bases, calendar dates and reminder settings.
Compatible pilots cover the Hydration entry amount, WeeklyGoals reminder settings
and BodyMetrics measurement days. Product-unit quantities, fractional recipe
servings and profile measurement updates now have distinct mutation types, and
Billing webhook processing uses its own lifecycle enum. All six implementations
have completed their compatibility and provider checks.

## Inventory scope

The initial inventory at `a87e311f2` covers 4,402 production C# files in 296
projects and all 34 logical modules. A Roslyn syntax scan recorded 17,720 explicit
primitive properties, fields and parameters, with no syntax errors. This number
includes constants, DTOs, projections, provider fields and private representations
inside value objects; it is not a defect count.

Tests, generated API clients, migrations, designer files and build outputs are
excluded. Independent MailRelay, MailInbox and BugTriage services, executable
hosts, shared libraries, integration adapters and development tooling are
included. The reproducible inventory and source locations are in
`.artifacts/primitive-audit/backend-primitives.json`; the reviewed module matrix
below is the durable migration plan. The committed
[inventory summary](primitive-types-inventory.json) preserves counts and layer
coverage for each module, independent service and platform/shared/tooling group.

## Where primitive representations are appropriate

Keep numeric/string HTTP DTOs, database columns and read projections compatible.
Convert at the owning application/domain boundary. An existing value object may
contain numeric fields without suffering primitive obsession: `ProductNutrition`
and `RecipeNutrition` group nutrients and reject invalid values already.

External provider identifiers, status strings, model names, source measurement
labels and raw import data need their provider semantics. A closed internal enum
must not accidentally reject a future external provider status. An opaque hash
or external identifier is not automatically a candidate for UUID conversion.

Use a new type when it prevents a meaningful mix-up, centralizes a repeated
invariant, expresses valid combinations, or supplies domain operations. Ordinary
titles, descriptions, pagination numbers and diagnostic counters do not need
wrappers merely because they use a primitive CLR type.

## Existing safeguards

- Typed owner IDs live in owning Domain.Contracts projects or established narrow
  contracts; `IEntityId<T>` is the shared protocol. Foreign aggregate references
  remain prohibited.
- `ProductNutrition` and `RecipeNutrition` own grouped numeric validation;
  `RecipeNutritionPolicy` owns scaling, precedence, nullable totals and rounding.
- Users owns `ProfileWeightKg`, `ProfileHeightCm`, `DesiredWeightKg`, `DesiredWaistCm`, profile update
  values and guarded mutations. Shared `EmailAddress` owns email normalization.
- Cycles already uses `DateOnly` for calendar dates. UTC guards and injected clocks
  cover many instant-based flows.
- Hydration, BodyMetrics, Exercises, WeeklyGoals and Fasting validate their
  primitive inputs today. Additional types improve expression and caller safety;
  their introduction must preserve those rules.
- Billing already guards `numeric(19,3)` and three-letter currency codes. Provider
  amounts must never be rounded or changed to fit a new abstraction.

## Priority findings

| Priority | Boundary | Current representation and risk | Next type or design | Compatibility requirements |
| --- | --- | --- | --- | --- |
| First pilot | [Hydration entry](../../Modules/Hydration/Domain/Entities/Tracking/HydrationEntry.cs) | An entry amount is an `int` with a repeated 1..10000 ml rule. | `HydrationAmount` for one entry, consumed by typed creation/update paths. | Keep integer `AmountMl`, existing errors, UTC normalization, audit stamps and permanent replay receipts. |
| Fourth verified implementation | [Meal items](../../Modules/Meals/Domain/Entities/MealItem.cs) and [recipe ingredients](../../Modules/Recipes/Domain/Entities/RecipeIngredient.cs) | Stored `double Amount` has two source meanings. | Separate owner `ProductUnitQuantity` and `RecipeServingQuantity` at every mutation entry point. Product units/bases remain attached to the source product or saved snapshot. | Preserve fractional precision, missing nutrition, historical snapshots and automatic/manual calculation behavior. Milliliters and pieces are not grams. |
| Fourth verified implementation | [Recipe nutrition inputs](../../Modules/Recipes/Domain/Nutrition/RecipeNutritionIngredient.cs) | Five competing scalar/nullable fields previously exposed both source alternatives. | Private construction now selects one source kind, nutrition values and a dimensionless scale factor. Typed factories distinguish product amounts and recipe servings; stored-source boundary adapters retain permissive read semantics. | Preserve current product-source precedence, stored fallback, partial nutrition and two-decimal rounding. |
| Third pilot | [Body metrics dates](../../Modules/BodyMetrics/Domain/Entities/Tracking/WeightEntry.cs) | Calendar measurement dates use UTC-midnight `DateTime`; instants elsewhere use the same CLR type. | Owner-local `MeasurementDay` with `DateOnly`, consumed by typed creation/update paths and the existing date normalizer. | Preserve selected day, existing UTC-midnight API encoding, PostgreSQL date columns, duplicate rules and query boundaries. No blanket timezone conversion. |
| Second pilot | [Weekly reminders](../../Modules/WeeklyGoals/Domain/Entities/WeeklyGoal.cs) | Enabled state plus nullable local-time minutes and offset minutes form a combined invariant. | `WeeklyGoalReminderSettings` with disabled/enabled states and distinct local time/UTC offset meanings, consumed by typed mutation paths. | Keep 0..1439 minute range, UTC-14..UTC+14 offsets, Monday/week rules and last-sent reset semantics. Do not replace offsets with timezone IDs implicitly. |
| Fifth verified implementation | [User profile](../../Modules/Users/Domain/Entities/UserNutritionProfile.cs) and [profile mutations](../../Modules/Users/Domain/Entities/User.Profile.cs) | Profile weight and height have different units; birth-date and goal semantics remain separate follow-ups. | UserPersonalInfoUpdate now consumes ProfileWeightKg/ProfileHeightCm immutable references. Their former zero struct defaults are closed; scalar compatibility uses the same validation/audit core. | Preserve omission and explicit birth-date clearing, profile/goal distinctions, nutrition/account partition versions and BMR/TDEE formulas. |
| Second wave | [Fasting plan](../../Modules/Fasting/Domain/Entities/Tracking/Fasting/FastingPlan.cs) | Alternative plan settings combine hour/day numbers and nullable fields. | Validated intermittent/extended/cyclic settings and scoped duration values. | Preserve protocol choices, supported bounds, state transitions, calendar anchors and reminder behavior. |
| Second wave | [Shopping quantities](../../Modules/MealPlanning/Domain/Entities/Shopping/ShoppingListItem.cs) | Optional amount and measurement unit travel separately. | An optional quantity/basis value consistent with source snapshots. | Preserve unmeasured/free-text items, rounding, sorting and source provenance. |
| Sixth verified implementation | [Webhook processing state](../../Modules/Billing/Domain/Entities/BillingWebhookEvent.cs) | Storage/read strings retain their codes; domain and processing decisions now use a closed owner enum. | BillingWebhookProcessingState classifies known states and unrecognized stored rows; typed known-state writes use one codec. | Keep persisted strings, retry saturation, timestamps and processed-event fencing. Provider payment/subscription status is a separate open contract. |
| Second wave | [Billing payment](../../Modules/Billing/Domain/Entities/BillingPayment.cs) | Amount and currency are nullable scalar values with existing guards. | Scoped currency/amount values where arithmetic or comparisons need them. | Preserve nulls, provider identifiers, refunds/negative values where allowed and three fractional digits. No global two-decimal money rule. |
| Second wave | [Report targets](../../Modules/ContentReports/Domain/Entities/ContentReport.cs) and [recent items](../../Modules/RecentItems/Domain/Entities/Recents/RecentItem.cs) | Kind plus raw `Guid` identifies several possible owners. | An owner-local target reference value carrying kind and ID together. | Preserve polymorphic target ownership and scalar persistence; do not introduce foreign aggregate navigation. |
| Later | [Notification](../../Modules/Notifications/Domain/Entities/Notification.cs) | Type string, JSON payload and optional reference describe an extensible delivery contract. | Typed application notification variants with a serialization boundary. | Preserve existing payloads and delivery compatibility; review extensibility before choosing a closed enum. |
| Later | [Image asset](../../Modules/Images/Domain/Entities/Assets/ImageAsset.cs) | Object key and URL are distinct string concepts. | Owner values at input/use boundaries if they simplify validation and prevent cross-use. | Preserve signed upload handling, current URL contracts and existing stored values. Do not tighten validation incidentally. |

## Module review matrix

High means substantial semantic value for a focused follow-up. Medium/later means
existing guards are useful and the benefit depends on a specific consumer. A
boundary-only area remains covered by the inventory without requiring new domain
objects.

| Module | Assessment and next action |
| --- | --- |
| Admin | Access/session reasons and technical IDs are bounded; retain filter/projection primitives. Reuse owner types when invoking business use cases. |
| Ai | Treat provider model names and token counts as provider/usage data. Keep typed recognition contracts; review prompt keys or quantity mappings when changing those flows. |
| Billing | Medium: internal webhook state and currency/amount pairs. Preserve open provider status and numeric(19,3) guards. |
| BodyMetrics | Third pilot: measurement day at mutation boundaries; weight and circumference mutations now consume distinct measured values. Existing numeric validation and calendar contracts are retained. |
| ContentReports | Medium: target kind/ID value. Existing typed report/user IDs and status enum are appropriate. |
| Cycles | DateOnly and owner enums already express major semantics. Follow-up types for lengths/ranges need benefit beyond existing guards. |
| DailyAdvices | Later: group ID and weighted selection inputs. Keep content text and bounded scalar weights. |
| Dashboard | Boundary/composition area: scalar read projections are appropriate; consume owner meanings rather than creating dashboard aggregates. |
| Dietologist | Existing invitation status, permissions and EmailAddress normalization are useful. Keep tokens/hashes opaque; scoped date/permission values are optional follow-ups. |
| Exercises | Second wave: typed duration minutes, burned kilocalories and recorded day protect mutations. Preserve current bounds and rounding. |
| Export | Boundary area: distinguish calendar-date and instant ranges at query policies; file bytes/text remain primitives. |
| Fasting | Medium: plan-specific settings and duration/range meanings; existing plan/protocol/status enums are already typed. |
| Favorites | Second wave: positive finite preferred product quantity retains its owner-specific unbounded contract. Existing owner IDs remain typed. |
| Gamification | Achievement keys are bounded catalog identifiers; thresholds depend on the metric. Use metric-specific values only where they prevent mixing. |
| Hydration | First pilot: validated entry amount; UTC timestamps and replay IDs remain unchanged. Daily goals/totals have separate limits. |
| Identity | Security-sensitive provider IDs and token hashes stay opaque. Existing session guards and owner IDs are retained. |
| Images | Later: object key versus URL. This is a type-design candidate, not a newly asserted URL/security defect. |
| Lessons | Text/content and read-progress timestamps have existing owner rules; no broad scalar replacement. |
| Marketing | Anonymous/session/provider attribution fields are external identifiers. A typed internal event category may help if its catalog is closed. |
| MealPlanning | High for quantity basis and servings; medium for shopping amount/unit combinations and plan day counts. |
| Meals | High for product quantity versus recipe servings and saved snapshots; grouped nutrition/IDs are already meaningful. |
| Notifications | Later for typed notification variants and reference contracts; retain extensible delivery semantics. |
| OpenFoodFacts | Reference/import boundary: retain source codes and units and normalize in owning application mappings. |
| Products | High for measurement basis/quantity; ProductNutrition and owner IDs already provide protection. |
| RecentItems | Medium for kind/ID reference; current timestamps and counts are guarded. |
| RecipeCommunity | Typed user/recipe relationships and guarded content are established; no broad replacement of text or counters. |
| Recipes | High for ingredient basis and serving counts; preserve nested recipe and nutrition policies. |
| Statistics | Projection/calculation boundary: consume owner units and date policies, leaving aggregate outputs scalar where useful. |
| Tdee | Calculation boundary: distinguish kilograms, centimeters, age and energy at inputs; retain established formulas and output representation. |
| Usda | Import/reference boundary: retain FDC IDs and source names; Amount versus GramWeight conversion is the meaningful review point. |
| Users | High for mixed optional measurements/date/profile settings; reuse existing owner value objects and preserve clearing semantics. |
| Wearables | Provider/import boundary: distinguish recorded days and instants; external user/token values remain opaque. |
| WeeklyCheckIn | Composition boundary: reuse owner measurements and reporting ranges; do not create duplicate aggregate models. |
| WeeklyGoals | Second pilot: immutable reminder settings at the mutation boundary. Week boundaries, owner IDs/type enum and DateOnly last-reminder date retain their contracts. |

Independent MailRelay has typed message IDs, retry/suppression policies and status
constants; a processing-state type is a later owner-specific improvement. Email
transport fields preserve RFC/provider semantics. MailInbox already has a typed
message ID/status and DateTimeOffset timestamps; malformed or missing inbound
addresses must remain representable before validation. BugTriage ingestion and
host/shared/infrastructure fields remain boundary/technical values unless a
specific business invariant establishes a new owner type.

## Hydration pilot

`HydrationAmount.FromMilliliters` validates the unchanged 1..10000 ml interval.
It is a sealed immutable record with a private constructor, value equality and
no implicit integer conversions. Unlike a positive-quantity struct with a public
default zero, an instance can only be produced through its validating factory;
typed entry creation rejects a missing reference.

The three owning application handlers now use `CreateWithAmount` or
`UpdateDetails`. Existing `Create` and `Update` primitive methods delegate through
the validated amount and retain compatibility for existing callers. Domain state
and EF read queries continue using the mapped integer `AmountMl`; adding the type
does not create a new database entity or column. This is an initial typed
mutation boundary, not a completed replacement of every quantity representation.

Keep existing authorization and application validation ahead of domain creation.
Raw create validates user ID before amount; raw update validates amount before
timestamp and applies no quantity mutation when timestamp validation fails. No-op
updates retain their audit behavior. UTC normalization, permanent receipt matching
and PostgreSQL microsecond timestamp handling are unchanged.

Pilot evidence lives in owner tests:

- `HydrationAmountTests`: accepted boundaries, invalid/extreme integers and value equality.
- `HydrationEntryTypedAmountTests`: typed creation/update, missing amount, no-op state,
  timestamp-only update, atomic failed update and compatibility validation order.
- Existing Hydration domain/application tests retain amount/error/ownership/replay cases.
- `HydrationAmountMappingTests` checks the provider model's integer column and
  existing SQL amount constraint without requiring a database connection.
- `HydrationTypedAmountPersistenceTests` creates, reloads and updates an entry
  through the typed quantity using the existing PostgreSQL integer column.
- Existing Hydration PostgreSQL repository/receipt tests check actual persistence
  and pending-model equivalence when Docker is available.

## WeeklyGoals reminder pilot

`WeeklyGoalReminderSettings` groups the enabled state, local `TimeOnly` and UTC
`TimeSpan` offset in a sealed immutable record. Disabled settings contain neither
value. Enabled settings normalize local clock seconds to the existing whole-minute
precision and accept only whole-minute offsets within UTC-14..UTC+14. Non-whole-hour
offsets such as UTC+05:45 and UTC-03:30 remain valid. There are no implicit numeric
conversions or timezone-ID/DST changes.

`UpsertWeeklyGoalCommandHandler` converts its validated input into settings and
uses `CreateWithReminder` or `UpdateWithReminder`. The primitive `Create`/`Update`
adapters retain their domain validation order, exception parameter names and
disabled-input normalization. Target-only changes preserve `LastReminderLocalDate`;
reminder configuration changes reset it; no-op updates preserve the audit stamp.
An invalid update timestamp cannot partially mutate the goal.

Persistence and read projections retain the existing boolean and nullable integer
minute columns. Reminder dispatch still uses the existing persisted-state checks,
local-date calculation, batching, cancellation and notification/save behavior.
`MarkReminderSent` retains its local-day/week validation and defensive handling
of a missing stored offset.

Evidence lives in `WeeklyGoalReminderSettingsTests`, `WeeklyGoalTypedReminderTests`,
the existing owner domain/application suites, and
`WeeklyGoalTypedReminderPersistenceTests`. Added application cases exercise clock
seconds, disabled extraneous fields and local-week boundaries at UTC+05:45,
UTC-03:30 and UTC±14 with once-per-local-date delivery. The PostgreSQL round trip verifies the unchanged
model snapshot and scalar storage, sent-date persistence and clearing on disable.
Real-host OpenAPI snapshot checks protect the unchanged public API contract.

## BodyMetrics measurement-day pilot

`MeasurementDay` is an owner-local readonly record struct holding `DateOnly`.
The typed `CreateForDay` and `UpdateDetails` mutation paths for weight and waist
cannot accept an arbitrary `DateTime` in place of a calendar day. The default
value is the already-representable minimum day, not an invalid positive quantity.
There are no implicit DateTime conversions or new minimum/future-date restrictions.

`FromDateTimeEncoding` preserves the existing compatibility rule: Unspecified
input retains its written calendar day; Local input retains UTC-date extraction;
UTC input retains its UTC day. `ToUtcDateTime` explicitly encodes the date as UTC
midnight. This encoding does not turn the selected calendar day into a user's
local-midnight instant. The four write handlers use the same day for duplicate
lookups and typed mutation; `UtcDateNormalizer` delegates to the same rule for
unchanged history-query bounds.

Primitive aggregate adapters, numeric ranges/precision, owner validation,
optional/date-only/numeric-only updates and audit/no-op behavior are compatible.
Weight creation still conflicts for an occupied day; identical waist creation
still returns the existing entry. Access checks and repository user predicates
remain in place. Goals continue to belong to Users.

The mapped DateTime fields, PostgreSQL `date` columns, unique user/day indexes,
serializer, HTTP DTOs and read projections retain their contracts. Provider reads
can return an Unspecified DateTime for a SQL date as before; rehydration through
the explicit compatibility adapter preserves that calendar day. No schema
migration is required.

`MeasurementDayTests` cover calendar/clock meaning, leap day, year and DST-date
boundaries, UTC/Unspecified/Local inputs, minimum/default and maximum days.
`TypedMeasurementDayTests` protect typed mutation, numeric failures, identity and
audit behavior. `MeasurementDayCommandTests` verify lookup/mutation/response day
agreement, cancellation propagation and distinct duplicate behavior. Existing
owner suites retain access, range and history cases. The shared PostgreSQL suite
exercises owner-context round trips, unchanged model/index/column identities and
user isolation; existing competing-write tests retain rollback/conflict evidence.
Public OpenAPI snapshots and the maintained nine-zone frontend calendar runner
protect the compatibility boundary.

## Product quantity and recipe servings implementation

`ProductUnitQuantity` belongs to Products Domain.Contracts; `RecipeServingQuantity`
belongs to Recipes Domain.Contracts. Both are sealed immutable records with
private construction, finite positive values through 1,000,000, fractional
precision and no implicit double conversions. They describe consumed quantities;
a recipe's integer total yield and a product's base amount remain distinct.

The product quantity is in its owning product's declared units. The product or
historical snapshot retains the unit/base descriptor. The value does not claim
that milliliters or pieces are grams and does not add an inferred mass conversion.

`Meal.AddProduct`/`AddRecipe` and `RecipeStep.AddProductIngredient`/
`AddNestedRecipeIngredient` accept only their respective quantity types. Raw double
overloads have been removed from these internal domain contracts after all source
callers were migrated, including manual input, duplicate/repeat flows and test
fixtures. This requires a coordinated repository rebuild, with compatible HTTP
and persistence shapes. Item/ingredient updates are also source-specific and
reject the opposite item kind before mutation. Stored `Amount`, comparison
tolerance, IDs, ordering, audit fields and snapshots retain their semantics.

`RecipeNutritionIngredient` privately constructs one selected source plus its
dimensionless scale factor. Product and recipe factories are distinct. Stored
boundary adapters retain the previous permissive numeric read contract and
positive-product-base precedence over nested yield, even when product nutrients
are missing. The policy retains division before multiplication, source iteration
order, incomplete-versus-zero behavior, stored fallback and two-decimal ToEven
rounding. No new validation is imposed on historical calculation projections.

`NutritionQuantityBoundaryTests` guards typed entry-point ownership, absence of
raw amount writers and implicit conversion/default escapes, and closed calculation
input construction. Owner tests cover bounds/non-finite values, fractional
servings, wrong-source mutations, no-op state and independent nutrition examples.
Provider round-trip, saved-snapshot and composed-reader verification passes on
PostgreSQL. Existing meal projection cases now also preserve fractional product
quantities and recipe servings through their saved snapshots.

## Typed profile measurement updates

Existing owner `ProfileWeightKg` and `ProfileHeightCm` values are now sealed
immutable records instead of positive structs with an invalid public default
zero. Factories retain their existing finite-number rules, 500 kg/300 cm limits,
fractional precision and messages. `UserPersonalInfoUpdate` carries their distinct
nullable references; a missing reference remains an omitted measurement.

The owning UpdateUser handler maps scalar validated input into these values.
Typed updates and the seven-scalar compatibility overload share one private
mutation core, preserving deletion/text/birth/measurement/gender validation,
parameter names, optional field behavior and profile/account audit ownership.
`BirthDateSpecified` still clears a null birth date independently of omitted
measurements. Nutritional measurements touch UserNutritionProfile; no-op updates
preserve timestamps and measurement changes do not invalidate account security.

Wire DTOs, persistence/state numbers, goal meanings and BMR/TDEE formulas retain
their contracts. Existing User domain/application suites plus
`TypedPersonalInfoMeasurementTests` cover typed values, omission, explicit date
clearing, partition audits, scalar validation order and default safety. The
architecture guard locks the patch field types and construction boundary.
The complete Users PostgreSQL suite passes, including independent profile-state
versions and projections. Its consumer-profile case persists typed fractional
kilograms and centimeters and verifies the existing scalar read results.

## Internal billing webhook processing state

`BillingWebhookProcessingState` belongs to Billing Domain. It classifies the
internal Received/Failed/Processed lifecycle and explicitly identifies an
Unrecognized legacy storage value. Domain transitions and the two processing
flows now compare typed state; known status writes pass through one private typed
codec. This enum does not classify payment/subscription provider statuses.

The existing mapped `Status` string retains exact ordinal `received`, `failed`
and `processed` codes. Unknown/case-mismatched stored values retain their original
read representation and prior nonterminal transition behavior. The computed
`ProcessingState` is explicitly ignored by EF, so column/index/SQL-filter and Admin
projection contracts retain their shapes. No row backfill or migration is needed.

Processed events retain their early no-op fence and refusal to become failed.
Chronology validation, attempt overflow saturation, exponential delay/cap,
timestamp saturation, failure fields, transaction ownership, authentication and
provider money/currency handling are unchanged. Owner lifecycle tests, all Billing
application/provider-adapter/controller suites, metadata shape tests and the
architecture usage guard verify this boundary. Real PostgreSQL workflow execution
also passes, including transaction rollback, persisted backoff, concurrent ordering
and processed-event fencing.

## Verification of the current implementation wave

Verification on 2026-10-08 used an isolated complete solution build with no warnings
or errors, followed by focused owner and cross-module runs. All selected tests
passed without skips: 3,358 unit tests across 32 projects, 237 PostgreSQL/provider
tests across seven projects, 2,159 architecture tests and four real-host OpenAPI
snapshot tests. The maintained calendar runner passed 675 cases in nine zones.
C# formatting, architecture health and the NuGet vulnerability audit also passed.

The PostgreSQL runs cover Meals, Users, Products, MealPlanning, Hydration,
WeeklyGoals and the affected central recipe, composition, model, migration and
Billing workflows. Public HTTP snapshots and the EF model remain compatible;
no database migration or data backfill is required. Internal mutation signature
changes require the normal coordinated repository rebuild.

Detailed logs, TRX results and test-selection summaries are retained locally in
`.artifacts/primitive-types-completion-20261008/`. Further candidates in the module
matrix remain follow-up work; this verification closes the six implementations
described above.

## Follow-up acceptance rules

1. Name the unit, basis, temporal meaning or state invariant before introducing a type.
2. Keep validation errors, normalization, precision, null/absence behavior and check order.
3. Choose a narrow owning module/contract. Add a shared value only when the same
   meaning and invariant are genuinely shared by production consumers.
4. Keep wire/storage shapes stable through explicit mappings; verify EF model
   equivalence and HTTP contracts. Add migrations only for intentional schema changes.
5. Exercise wrong-unit/type use, invalid/default values, rollback/no-op behavior
   and real provider round trips where that boundary changes.
6. Migrate consumers deliberately. Compatibility adapters are transitional seams;
   record their remaining callers before removing them.
7. Avoid incidental changes to double/decimal representation, nutrition rounding,
   UTC/calendar handling, provider vocabularies or historical snapshots.

## Second implementation wave

The follow-up after `429cf221c` implements the eight requested boundaries:

| Boundary | Implemented contract | Compatibility seam |
| --- | --- | --- |
| Frontend identities and time | Owner-branded entity IDs and distinct CalendarDate/UtcInstant in application models, SDK mappers and mutation services/capabilities. | Explicit raw route/SDK/placeholder tagging preserves strings, timestamp precision and existing calendar encodings. |
| Frontend meal sources | Product/recipe MealItem branches require ProductQuantity/RecipeServings; historical dual sources are an explicit legacy variant. | Deleted sources, permissive stored observations and product precedence retain their read behavior; transient forms and wire DTOs remain scalar. |
| Goal and measured values | Desired weight/waist references cannot default to zero; separate MeasuredWeightKg/MeasuredWaistCm feed goal history and BodyMetrics. | Null/omission, no-active-goal cancellation, fractional precision, numeric bounds and partition audits remain compatible. |
| Fasting modes | Closed immutable intermittent/extended/cyclic settings, paired DailyFastingWindow and FastingCycleDay. | Nullable stored fields, existing protocols, anchors, schedules and historical fallbacks remain supported. |
| Shopping and favorites | ShoppingQuantity, ShoppingSourceQuantity and PreferredProductQuantity express their separate invariants. | Optional amount/unit combinations, unbounded source/preference amounts, legacy source units, source provenance and free-text items remain valid. |
| Polymorphic targets | ReportTarget and RecentItemReference pair kind with source-specific owner ID factories. | Guid storage, empty/unsupported read probes, aggregate mutation validation and user-scoped access remain unchanged. |
| Exercises | ExerciseDay, ExerciseDuration and BurnedEnergy protect owning mutation inputs. | Whole-minute bounds, finite energy bounds, ToEven rounding and legacy date encoding are preserved. |
| Billing | Distinct internal subscription/payment/webhook IDs and nullable financial observation values protect ports and payment flows. | Guid persistence, external provider IDs/statuses, partial/negative money and three fractional digits remain unchanged. |

RecipeCommunity Domain.Contracts now owns only RecipeCommentId/RecipeLikeId.
ReportTarget can consume these scalar IDs without acquiring a foreign aggregate
dependency. The dependency matrix records the direct owner references. This is
an internal assembly move requiring a coordinated repository rebuild; Guid
converters, database relations and external HTTP contracts retain their shape.

Validated value records have private construction and immutable properties.
Scalar compatibility entry points remain available for established callers;
owning production mutation paths use their typed counterparts. EF explicitly
ignores computed Billing semantic references. RecentItems' tracked fallback
creates through its typed reference; SQL batching, pruning and transaction
capabilities retain their reviewed implementation.

Owner regression tests cover distinct target/observation values, default safety,
partial quantities and money, mode settings, bounds, chronology and no-op state.
SemanticMutationBoundaryTests protects validated construction, typed mutation
signatures and closed modes. Frontend compiler tests deliberately pass wrong
owner IDs, day/instant values, source quantities and branch combinations and
require rejection; runtime tests preserve encodings, absence and historical
values. Verification logs for this wave live in `.artifacts/primitive-types-wave2/`.

## Second-wave verification

On 2026-10-08 the complete solution and all frontend verification stages passed.
The selected backend runs passed 6,692 tests without failures or skips:
4,040 owner/consumer unit tests, 130 owner provider tests,
all 324 central PostgreSQL integration tests, 2,194
architecture guards and four real-host OpenAPI exports. The full central run
verifies unchanged model/schema and relational behavior, including unsupported
report probes returning false without opening the database.

Frontend app/admin/UI-kit/tour suites passed 5,529 tests. Compiler
fixtures reject wrong IDs, calendar/instant meanings, cross-family rebranding and
source quantities; nine-zone calendar checks passed 675 cases.
Ten controlled Playwright scenarios passed at mobile/desktop widths, including
meal failure/retry/discard, favorites, goals, water and keyboard history paths.
These browser fixtures do not exercise the notification backend; their SignalR
negotiation errors are expected and do not represent live account validation.

NuGet vulnerability checks passed. Npm production dependencies have zero audit
findings. The full npm audit reports 14 pre-existing high dev-tool transitive
findings rooted in braces/micromatch, with no available braces fix. Dependency
versions and the npm lockfile are unchanged; this remains separate maintenance.
Detailed command logs, TRX files and selections are in
`.artifacts/primitive-types-wave2/`; browser screenshots are stored outside the repo.

## Third implementation wave

The repository-wide follow-up implements the fourteen audited boundaries. Raw HTTP,
SDK, provider observations and persisted fields keep their scalar representations;
owning workflows decode and consume semantic inputs.

| Boundary | Owning production contract |
| --- | --- |
| Optional updates | FieldChange<T> and Product/Recipe/User changes distinguish omission, set and clear; supplied blank text retains owner normalization. |
| Notification intent | Closed intent factories couple payload/type/target; composite recommendation targets share a codec, with explicit unknown legacy requests. |
| Product basis | ProductMeasurementBasis and per-unit ProductDefaultPortion protect create/update/duplicate and preserve atomic nutrition normalization. |
| Remaining frontend meanings | Profile, goal-history, dietologist/recommendation/cycle owner IDs and day/instant models; separate admin meanings and decoded catalog sources. |
| AI usage | Validated AiTokenUsage permits provider overhead; inconsistent/overflowing counts use established absent-usage/estimated reconciliation. |
| Wearable readings | Metric-specific readings and sync days retain fractional doubles, source bounds and provider validation timing. |
| BugTriage identity | Report/source-message/lease-token types protect store fences; explicit HTTP mapping preserves scalar Guid leases. |
| Achievement targets | Metric-specific targets feed definition mutations and actual eligibility/grant calculations without extra caps. |
| Calculation inputs | BMR measurement groups and distinct TDEE energies/measured/desired weights preserve historical fallback and formulas. |
| Planned quantities | Duration/day values retain 1..31 rules; positive integer servings remain unbounded and distinct from fractional recipe quantities. |
| Prediction output | Owner classifications/reasons and optional windows feed revision recording while retaining unknown/versioned/partial rows. |
| Mail lifecycle | Internal queue enum/codec and coupled Retry/Failed decisions preserve storage codes and external event vocabulary. |
| Image locations | Keys/public/signed URLs protect ports; frontend selection variants retain empty, remote, uploaded and legacy asset-only states. |
| Period contracts | Statistics instant/body periods and Export diary/calendar periods retain timezone, offset, limit and inclusivity policies. |

Scalar compatibility entrypoints remain available for established external callers.
SemanticMutationBoundaryTests guards owning consumer adoption and immutable
construction. Frontend compiler-negative fixtures reject wrong owners, day/instant
swaps, public/upload URL swaps, selection contradictions and catalog quantity swaps.
Verification artifacts for this wave live in `.artifacts/primitive-types-wave3/`.

## Third-wave verification

The complete solution builds with zero warnings or errors. Changed C# sources
pass whitespace verification; project formatting and all 2,214 architecture guards
pass. The executed backend selections pass 9,485 tests without skips or failures:
6,107 unit tests across 80 owner/consumer projects, 213 provider/service tests,
324 central PostgreSQL tests, 276 complete real-host API tests and 351 shared
presentation tests, plus the architecture suite. The separately selected four
OpenAPI checks are included in the full host count. API compatibility reports zero
structural or behavioral changes; generated SDKs remain unchanged.

Every `npm run verify` stage passes. Main/admin/UI-kit/tour tests total 5,534;
675 maintained calendar cases pass in nine timezones, including DST and unusual
offsets. Thirteen controlled Playwright cases verify profile/cycle navigation,
recipe/gallery/text editing, admin account navigation and actual catalog
preview/import ordering at 390/1280 widths. Catalog screenshots retain readable
Russian fixture names without clipping. These owned browser fixtures do not
exercise the notification backend; their client SignalR negotiation errors are
expected. Notification/provider regressions run in their backend owners.

NuGet vulnerability checks are clean. Npm production audit reports zero findings;
the full audit retains 14 existing high dev-tool advisories rooted in braces /
micromatch with no available braces fix. External dependency versions and the npm
lockfile are unchanged. Detailed execution logs, TRX files, the per-requirement
audit and summary are stored under `.artifacts/primitive-types-wave3/`; browser
screenshots and temporary scripts stay outside the repository.

## Further boundary refinements

Telegram operation journals use distinct `TelegramOperationId`, `TelegramLeaseId`
and the owning `UserId` throughout commands, store ports and handlers. The bot
keeps its own operation, lease and user types without backend dependencies.
Explicit HTTP and persistence adapters retain GUID encodings, parameterized SQL,
lease fencing, retry/expiry rules and per-operation encryption purposes. Compile
negative cases reject identity swaps; PostgreSQL regressions cover recovery,
stale writers, cancellation, cleanup and shared-transaction rollback.

Nested cycle mutation inputs use `CalendarDate` and `CycleFactorId`. Transient
form values are decoded after the existing calendar-key conversion; omission,
clear flags, nullable end dates and date encodings retain their existing behavior.
Factor identity remains typed through selection state and component events.

Notification UI models decode `NotificationId`, `UtcInstant` and a known/unknown
notification kind at the SDK boundary. Realtime invalidation reloads through that
same decoder. One exhaustive presentation table supplies flags, icons, badges
and actions. Unknown codes retain the generic presentation and their original
code; navigation continues to use the server-provided target URL, independently
of the reference string.

Meal-plan, plan-day, planned-meal, lesson and food-recognition identities remain
typed through UI models, selection state, component events and owning API calls.
Routes, SDK responses, stored pending task IDs and realtime events attach meaning
at their boundaries without changing scalar encodings. Recognition snapshots
also distinguish image assets, displayed image URLs and UTC timestamps; internal
polling and session checks retain the user identity role.

Planning duration/day values use distinct 1..31 meanings. Planned servings are
positive integers without the consumption cap; stored projections preserve
historical values. A shared numeric marker prevents retagging consumed servings
as planned servings, durations or day ordinals. Meal type codes preserve their
casing, unknown values and existing diary-selection fallback.

Attribution uses distinct opaque `AnonymousVisitorId` and `MarketingSessionId`
values and the existing `UserId` in internal commands, records, owner state and
lookups. EF converters retain varchar(96), nullable UUID columns, indexes and
exact stored values. HTTP and consumer models unwrap their original scalars;
event vocabulary, timestamp parsing, normalization/fallback order, retention and
stable premium-conversion IDs remain unchanged. Visitor/session SQL distinct
counts remain separate. Browser decoding retains local visitor lifetime, session
lifetime, capture keys and first-touch precedence, including legacy non-UUID
stored identities. Compiler-negative cases reject identity-role swaps.

Fasting reminder updates carry one `FastingReminderDelayUpdate`. The User facade
retains individual-field and appearance validation order, merges omitted fields
against current settings and checks follow-up ordering before applying or auditing
changes. Immutable `FastingReminderSettings` owns valid 1..168 elapsed-hour pairs
and defaults 12/20. `FastingReminderSchedule` explicitly preserves legacy stored
fields while exposing the original sorted/distinct due sequence; equivalent
schedules have value equality so no-op updates remain unaudited. Users.Contracts
passes that schedule to Fasting repositories and planners without a Users.Domain
dependency. Reference codes, persisted fields and HTTP payloads retain their
existing representations; scalar compatibility constructors decode into groups.

Users personal-info field changes and measured BMR inputs use immutable
`ProfileBirthDate`. It exposes the encoded calendar day while retaining original
DateTime ticks/Kind for the profile owner's existing UTC-midnight normalization.
Decoding does not move future validation or alter its ordering. BMR uses that
calendar day with the established birthday/AddYears age policy; stored-profile
and scalar calculation adapters preserve historical missing/invalid measurements
and date encodings. Set/clear/omission, profile-only audit no-ops, wire fields and
persisted DateTime columns retain their existing behavior.

Identity owns `RefreshTokenSessionId` with no implicit GUID conversions. Session
entity keys, lifecycle requests, repository models/ports and JWT/token-service
internals require it; lifecycle owners use `UserId`. EF converters and explicit
claim/HTTP adapters retain UUID columns, GUID strings, cookies and safe session
response fields. Rotation still fences by owner, active session and expected
hash; revocation still verifies the current active owner session and excludes
it when revoking others. Concurrent logout, legacy token paths, previous-token
grace, expiry and security-version behavior remain. Provider tests prove no
schema delta and the reviewed technical repository fingerprint remains exact.

Recipe ingredients decode at the SDK boundary into product, nested-recipe and
text alternatives with owning IDs and distinct quantity meanings. Explicit
legacy observations retain missing/deleted or contradictory sources, original
snapshots and each consumer's established precedence. Ingredient quantities
do not acquire meal-consumption caps; forms and outgoing DTOs retain scalars.

Recipe serving mass is either known finite positive grams per serving or
unknown. Display amounts carry grams plus the mass used for conversion, or
servings when mass is unknown. The conversion core requires those meanings;
raw form fields decode with the currently displayed unit. Complete gram-only
ingredient mass, lookup/error fallback, content-key invalidation, fractional
precision and transient form validation remain. Milliliters and pieces never
imply gram mass.
