# Admin scalar boundaries

Admin Application consumes BillingProviderNames, ReportStatus and AchievementMetric
through Billing, ContentReports and Gamification Domain.Contracts, respectively.
ContentReports Contracts consumes ReportStatus, ReportTargetType and ContentReportId from its scalar
owner, without exposing the aggregate-bearing Domain assembly.

The five source files retain their namespaces, members and values verbatim, including
BillingProviderNames.IsSupported. Direct consumers reference their exact owners.
Billing and Gamification contracts have no project dependencies; ContentReports contracts reference only Domain.Primitives for IEntityId. Existing foreign aggregate reads
in Infrastructure remain unchanged; no EF mapping, migration, API, authorization,
provider or runtime composition behavior changes. Rebuild all hosts together because
CLR assembly ownership changes. Docker restore and publish inputs include the new
projects. Roll back source placement and consumer references together.

Architecture guards enforce exported types, source ownership and removed edges.
Existing owner and Admin tests cover behavior; Swagger and EF model guards cover
wire/schema stability. Verification results live in .artifacts/admin-scalars-verification.

The boundary audit also found AchievementDefinition length constants and an unused aggregate mapping overload in Admin. AchievementDefinitionLimits owns the unchanged five limits; aggregate aliases preserve existing consumers and EF configuration. Admin validation consumes the narrow limits. The aggregate ContentReport mapping had only a test caller; Admin keeps the existing owner read-model mapping, now tested including reviewer and target metadata. No production read/mutation flow changed.
