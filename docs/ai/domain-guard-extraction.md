# Generic domain validation ownership

`FoodDiary.Domain.Primitives.DomainGuard` is a public API in the existing shared
Primitives library. It owns enum membership, finite/range checks, required/optional
text, JSON syntax validation and UTC normalization. It has no module, application,
EF, HTTP or provider dependency. Existing entity audit timestamps retain their
separate strict UTC contract.

Billing owns internal `BillingDomainGuard` beside BillingPayment. It retains
numeric(18,2) bounds, the ToEven scale comparison and three-ASCII-letter currency
normalization. Text normalization delegates to the public generic guard. There
is no currency registry lookup or new financial rule.

The old central guard and all central Domain friend grants are removed. Source
audit found no other internal types or members there. All fourteen external
production guard consumers reference Primitives directly. Cycles, Exercises,
Hydration, MealPlanning, Notifications, RecentItems, Wearables and Nutrition no
longer reference central Domain. Nutrition references only Primitives and Products
Domain.Contracts. Billing retains central constants; Dietologist retains
EmailAddress; Meals retains constants; Products and Recipes retain constants and
Visibility; Users retains shared values and constants. No unrelated central type
is moved and module-private guards retain their owners.

Method signatures, generic constraints, method bodies and validation order are
preserved. OptionalJson checks raw length before trimming; RequiredJson trims
before checking length and parsing. Malformed JSON retains its inner exception.
RequiredUtc rejects Unspecified but converts Local with ToUniversalTime. Decimal
scale validation rejects excess fractional digits rather than rounding inputs.
Consumer edits are imports and Billing guard qualification only.

Primitives owns focused generic contract tests. Billing tests exercise payment
creation through its public API; existing mixed tests retain their owners.
Verification includes the complete architecture suite, affected domain/application
suites, central consumers, full PostgreSQL and HTTP suites, EF pending-model check
and package audit. Evidence is retained under `.artifacts/domain-guard-evidence`.
This is a scoped source/behavior review, not a full security scan.

No EF mapping, database schema, HTTP payload, provider, job or deployment behavior
changes. Rebuild consumers and hosts together because the guard's assembly and
namespace have changed. Existing solution and Docker project lists remain valid:
no project is added or removed. Rollback is a coordinated code/package revert.
