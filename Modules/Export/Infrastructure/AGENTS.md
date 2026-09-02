# Export infrastructure

Own PDF rendering, chart/image composition and the typed HTTP client used only for
report images. Depend on Export contracts and Meals read-model/enum owners, never
on central Infrastructure, DbContext, Resources or executable hosts.

Preserve rendering limits, cancellation, timeouts, image byte limits, DNS/IP
validation and connection pinning. Redirects and proxies remain disabled.
Compose with AddExportInfrastructure in hosts; localization is supplied through
IDiaryPdfReportTextProvider. No database, retained file or background job ownership.

Focused tests: Modules/Export/tests/FoodDiary.Modules.Export.Infrastructure.Tests.
