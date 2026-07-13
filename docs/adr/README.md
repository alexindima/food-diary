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
| [0004](0004-application-abstractions-project.md) | Application abstractions project | Accepted | 2026-05-21 |
| [0005](0005-api-contract-snapshot-policy.md) | API contract snapshot policy | Accepted | 2026-05-21 |
| [0006](0006-business-module-ownership-and-fasting-pilot.md) | Business-module ownership and Fasting pilot | Accepted | 2026-07-13 |
| [0007](0007-backend-side-effect-transaction-semantics.md) | Backend side-effect and transaction semantics | Accepted | 2026-07-05 |
| [0008](0008-product-recipe-read-model-query-paths.md) | Product and recipe read-model query paths | Accepted | 2026-07-05 |
| [0009](0009-executable-application-module-dependency-graph.md) | Executable Application module dependency graph | Accepted | 2026-07-13 |

## Creating A Record

1. Copy [`template.md`](template.md) to the next zero-padded sequence number.
2. Complete the context, decision drivers, considered options, decision, and consequences.
3. Link related or superseded ADRs and the tests or manifests that enforce the decision.
4. Add the record to this index and link it from relevant living documentation.
