# ADR 0053: USDA links support the full provider catalog

Status: Accepted

Date: 2026-10-06

## Context

USDA search and detail already combine the imported local SR Legacy catalog with
live FoodData Central results. A valid branded result can therefore have no local
UsdaFoods row. The product-link handler rejected that result, and the Products
foreign key also restricted links to the imported subset. Users could save a
product and then receive a source-link error. Daily micronutrients read only the
local catalog and could not cover such a product.

The local reference tables are read-only at runtime. The initializer owns their
bulk import; importing arbitrary provider rows during product creation would
violate that boundary and couple private product writes to reference maintenance.

## Decision

- Keep Products.UsdaFdcId as an indexed, nullable external FDC identifier. Remove
  its foreign key to UsdaFoods without changing its column, index or existing data.
- The USDA link handler checks the authenticated user's product access first.
  It accepts a local reference or a matching detail from the existing provider
  port. An unavailable, missing or mismatched provider result cannot create a link.
  Product writes still go through the narrow Products-owned link service.
- Daily micronutrients use local nutrients first and resolve each missing FDC ID
  once through the existing cached, cancellable provider port. Only gram-based
  meal items contribute; scale remains grams / 100. Ignore invalid nutrient
  amounts and duplicate nutrient IDs. Never send user IDs, meal quantities or
  private product fields to the provider.
- Bound each daily request to twenty distinct provider-only foods and one overall
  fifteen-second lookup budget. Return an explicit rate-limit or dependency error
  when that bound is exceeded; do not silently truncate nutrient coverage. Keep
  caller cancellation distinct from the internal deadline. The existing day-item
  bound and provider cache/admission limits remain in force.
- Keep local USDA reference tables read-only and add no project references,
  reverse aggregate navigation, endpoints or provider credentials.

## Deployment and recovery

Apply the migration bundle before deploying the application. Up drops only the
foreign key; the existing FDC index and all product/nutrition data remain intact.
Older application versions can run with the relaxed schema but still reject
provider-only links and omit their micronutrients.

Prefer rolling forward. A schema rollback clears only FDC IDs absent from the
local catalog before restoring the old foreign key. Those external source links
are lost on rollback; products, their macros and locally resolvable links remain.
Preserve a backup if those links must be recovered. This rollback does not remove
products or modify the reference catalog.

## Verification

Verify ownership-before-provider access, missing/mismatched provider rejection,
local lookup independence, one lookup per distinct FDC ID and gram scaling.
Rehearse the migration chain and Up/Down on isolated PostgreSQL databases, preserve
the FDC index, check the model snapshot and migration bundle, and exercise a real
provider-only import followed by a meal and daily micronutrient read.
