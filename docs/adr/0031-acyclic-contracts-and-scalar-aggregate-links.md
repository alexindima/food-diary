# ADR 0031: Acyclic contracts and scalar aggregate links

- Status: Accepted
- Date: 2026-09-06
- Extends: ADR 0030

## Context

Contract cycles survived physical module extraction. Foreign CLR navigations allowed
repositories to acquire mutable aggregates from other owners. A shared DbContext also
made its complete tracking and write surface available to module adapters.

## Decision

Require zero cycles in the combined Application API and direct service-contract graph.
Domain ID/value references are recorded by the exact project matrix separately; the
legacy Images.Contracts assembly is verified to export only ImageAssetId.

Consumers own the small capabilities they need. Users requests session revocation
from Identity and profile-image resolution/cleanup from Images. Password interfaces
and input limits belong to shared Authentication.Contracts; Identity keeps hashing.
Favorites owns source readers and immutable presentation models; Meals, Products and
Recipes implement them and return the original owner errors. Meals requests achievement
evaluation through its own capability implemented by Gamification. USDA owns meal
nutrition and product-link ports; Products returns its original Result errors. USDA
suggestion contracts no longer require referencing its Application implementation.
Notifications retains its legacy Dietologist.InvitationNotFound wire error locally.

Meals owns nutrition aggregation. Dashboard adapts that projection to its unchanged
public model; Cycles, Gamification and daily-calorie readers consume Meals directly.
Preserve UTC intervals, bucket grouping, quantization, null handling and rounding.

Module domains retain scalar foreign IDs, never foreign aggregate CLR navigations.
PersistenceModel uses typed HasOne<T>() mappings to preserve existing columns, keys,
indexes and delete behavior. Same-owner navigations remain. RecipeIngredient and
MealPlanMeal use transient immutable snapshots; assignment copies collections and
validates identity. Repositories batch-project source data without tracking it.
Fasting projects reminder settings beside its owned occurrence. Dietologist and
RecipeCommunity project public/profile fields through explicit scoped joins.

FD0015 rejects foreign EF tracking and write acquisition, including Set, Entry, Find,
AsTracking and bulk mutations. Foreign reads start with AsNoTracking. FD0016 requires
an exact LF-normalized source SHA256 for SaveChanges, Database and ChangeTracker escape
APIs. Compiler AdditionalFiles mirror the reviewed persistence capability manifest.
Review SQL, transactions, retry and tracker behavior before changing a fingerprint.
An exception never authorizes foreign module writes; shared audit persistence has a
separate exact-source exception for the reviewed interceptor. Composition roots and
shared Infrastructure remain separately governed by architecture tests.

## Compatibility and verification

Keep one database, DbContext, migration history and unit of work. No database ACL or
runtime sandbox is introduced. Compiler checks are local semantic guardrails; project
matrices, capability review, DI composition and integration tests remain necessary.

Rebuild hosts and consumers together after contract assembly moves. Preserve API
snapshots, error strings, authorization/privacy rules, cancellation and transaction
behavior. Removing CLR navigations changes EF metadata but requires no schema migration;
verify HasPendingModelChanges and existing PostgreSQL FK/cascade and projection tests.
Run analyzer positive/negative/exception cases, architecture guards and affected
application/domain/infrastructure suites in one consolidated verification batch.
