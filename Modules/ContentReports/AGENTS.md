# Content Reports Module Guidelines

ContentReports owns report creation, moderation capabilities, its aggregate, persistence ports/model, and adapter. Preserve legacy CLR namespaces, `FoodDiary.Application.ContentReports` assembly identity, central migrations, HTTP contracts, and Admin authorization boundaries.

ReportStatus and ReportTargetType live in `Domain.Contracts/Enums` with the stable
`FoodDiary.Domain.Enums` namespace and unchanged member values. Consumers using
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
