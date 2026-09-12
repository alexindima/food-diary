# ContentReports scalar domain contracts

Own ReportStatus, ReportTargetType and ContentReportId with unchanged CLR namespaces and values.
Reference only shared Domain.Primitives for IEntityId. Keep aggregates, persistence and providers outside.
Consumers reference this owner directly. Assembly moves require coordinated rebuilds.
