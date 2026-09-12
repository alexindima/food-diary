# Architecture Decision Records

Architecture Decision Records (ADRs) capture significant decisions that constrain the long-term design of FoodDiary. They explain why a decision was made at a point in time; living documentation under `docs/` describes how the system works now.

## Lifecycle

- Create an ADR when a decision changes system boundaries, ownership, deployment, data consistency, public contracts, or an enduring engineering policy.
- Do not create an ADR for routine implementation choices, migrations that merely apply an accepted decision, or temporary plans.
- Start with `Proposed`. Change it to `Accepted`, `Rejected`, `Deprecated`, or `Superseded` after review.
- Accepted ADRs are historical records. Correct mistakes and links, but use a new ADR to change or extend the decision materially.
- A replacing ADR names the record it supersedes. The older record is updated only to point to its replacement.
- Keep current inventories, module maps, and operational instructions in living documentation rather than appending them to an ADR.

## Index

| ADR | Decision | Status | Date |
| --- | --- | --- | --- |
| [0001](0001-modular-monolith-with-supporting-services.md) | Modular monolith with supporting services | Accepted | 2026-05-21 |
| [0002](0002-mailrelay-mailinbox-as-separate-services.md) | MailRelay and MailInbox as separate services | Accepted | 2026-05-21 |
| [0003](0003-presentation-models-live-in-presentation-api.md) | HTTP models live in presentation projects | Accepted | 2026-05-21 |
| [0004](0004-application-abstractions-project.md) | Application abstractions project | Superseded | 2026-05-21 |
| [0005](0005-api-contract-snapshot-policy.md) | API contract snapshot policy | Accepted | 2026-05-21 |
| [0006](0006-business-module-ownership-and-fasting-pilot.md) | Business-module ownership and Fasting pilot | Accepted | 2026-07-13 |
| [0007](0007-backend-side-effect-transaction-semantics.md) | Backend side-effect and transaction semantics | Accepted | 2026-07-05 |
| [0008](0008-product-recipe-read-model-query-paths.md) | Product and recipe read-model query paths | Accepted | 2026-07-05 |
| [0009](0009-executable-application-module-dependency-graph.md) | Executable Application module dependency graph | Accepted | 2026-07-13 |
| [0010](0010-meals-terminology-and-application-boundary.md) | Meals terminology and application boundary | Accepted | 2026-08-13 |
| [0011](0011-application-runtime-boundary.md) | Separate the Application Runtime boundary | Accepted | 2026-08-14 |
| [0012](0012-cycle-health-data-and-calendar-date-boundary.md) | Cycle health-data and calendar-date boundary | Proposed | 2026-08-17 |
| [0013](0013-read-only-sqlite-context-search-in-development-mcp.md) | Read-only SQLite context search in the Development MCP | Superseded | 2026-08-21 |
| [0014](0014-sql-first-development-context-with-json-fallback.md) | SQL-first development context with JSON fallback | Accepted | 2026-08-21 |
| [0015](0015-application-module-root-folder-structure.md) | Application module root folder structure | Accepted | 2026-08-28 |
| [0016](0016-logical-module-folders-and-fasting-extraction.md) | Logical module folders and incremental Fasting extraction | Accepted | 2026-08-29 |
| [0017](0017-hydration-logical-module-extraction.md) | Reproduce the logical-module extraction for Hydration | Accepted | 2026-08-29 |
| [0018](0018-weekly-goals-logical-module-extraction.md) | Extract WeeklyGoals while preserving CLR, EF, HTTP, and reminder compatibility | Superseded | 2026-08-29 |
| [0019](0019-weekly-goals-domain-extraction.md) | Extract the WeeklyGoals Domain project | Accepted | 2026-08-30 |
| [0024](0024-open-food-facts-logical-module-extraction.md) | Extract OpenFoodFacts while preserving provider, cache, EF, and HTTP compatibility | Accepted | 2026-08-30 |
| [0025](0025-bodymetrics-measurement-domain-extraction.md) | Extract the BodyMetrics measurement domain | Accepted | 2026-09-01 |
| [0026](0026-recent-items-bounded-context-extraction.md) | Extract the RecentItems bounded context | Accepted | 2026-09-01 |

| [0027](0027-retire-shared-domain-assemblies.md) | Retire residual central and Nutrition domain assemblies | Accepted | 2026-09-02 |
| [0028](0028-retire-central-application-abstractions.md) | Retire the central application abstractions aggregator | Accepted | 2026-09-05 |
| [0029](0029-reviewed-persistence-capabilities-and-narrow-adapters.md) | Review persistence capabilities and narrow module adapters | Accepted | 2026-09-05 |

## Creating A Record

1. Copy [`template.md`](template.md) to the next zero-padded sequence number.
2. Complete the context, decision drivers, considered options, decision, and consequences.
3. Link related or superseded ADRs and the tests or manifests that enforce the decision.
4. Add the record to this index and link it from relevant living documentation.

- [0030: Owner lifecycle and transaction boundaries](0030-owner-lifecycle-and-transaction-boundaries.md)

- [ADR 0032: Retry isolation and image reference integrity](0032-retry-isolation-and-image-reference-integrity.md)

- [0035: Runtime boundaries and owner projections](0035-runtime-boundaries-and-owner-projections.md)

- [0036: Independent bug triage service](0036-independent-bug-triage-service.md)

- [0037: Telegram client identity and operation boundaries](0037-telegram-client-identity-and-operation-boundaries.md)
