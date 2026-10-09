# ContentReports scalar domain contracts

Own ReportStatus, ReportTargetType and ContentReportId with canonical project-relative namespaces and unchanged values.
Reference shared Domain.Primitives for IEntityId and the narrow Recipes/RecipeCommunity Domain.Contracts for typed report target IDs. Own ReportTarget with private construction and source-specific factories; FromFields preserves empty IDs and unsupported kinds for read probes returning false without database access. Aggregate writes still reject empty IDs and unsupported kinds. Keep aggregates, persistence and providers outside.
Consumers reference this owner directly. Assembly moves require coordinated rebuilds.
