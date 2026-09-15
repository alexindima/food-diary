# Content Reports Module Guidelines

ContentReports owns report creation, moderation capabilities, its aggregate, persistence ports/model, and adapter. Use canonical `FoodDiary.Modules.ContentReports.<Project>` assembly names and project-relative namespaces. Keep Application.Abstractions and PersistenceModel as sibling projects. Preserve central migrations, HTTP contracts, and Admin authorization boundaries.

ReportStatus and ReportTargetType live in `Domain.Contracts/Enums` with the canonical
`FoodDiary.Modules.ContentReports.Domain.Contracts.Enums` namespace and unchanged member values. Consumers using
these types reference ContentReports Domain.Contracts explicitly. Consumer Contracts
must not reference the aggregate-bearing Domain project. Preserve string EF
conversions and HTTP strings; moving the assembly owner requires coordinated
consumer rebuilds. Central FoodDiary.Domain must not reference this module.

## Scalar persistence boundary

PersistenceModel uses Users.Domain.Contracts for UserId. Its foreign User Cascade
relationship is composed by ContentReportsCrossModuleRelationships in central
Infrastructure after owned models. Keep local mappings and same-owner relationships
unchanged; do not restore a Users.Domain dependency to the model.

ContentReportId also belongs to Domain.Contracts, referencing shared Domain.Primitives for IEntityId. Preserve Guid conversion, formatting and Empty semantics.

ContentReports owns a single-entity runtime context and report writes. The host
composition implements its existing read-model and target-read ports, preserving
visibility predicates, SQL paging and bounded title/comment excerpts. No module
references the composition implementation; central migrations remain (ADR 0040).

Admin dispatches moderation and administration-read requests from Contracts. Review/dismiss handlers preserve pending-state checks and caller-owned saving; queries retain paging and filters.

Keep single-operation moderation logic in its handler. Caller-owned saves and rollback remain unchanged. Translate only the exact public ContentReports UserId/TargetType/TargetId unique constraint to a concurrency conflict; never translate unrelated database failures.

Review and dismiss request validators enforce the 2000-character AdminNote limit after trimming, preserving null/blank notes. Keep owner aggregate invariants and their tests in ContentReports; HTTP validation must not rely on domain exceptions.

Application-only error factories belong to Application/Common and remain internal. Aggregate types live directly in Domain/Entities. Moderation behavior tests belong to this module; Admin tests verify owner-request dispatch without constructing moderation handlers.
