# Export infrastructure

Own PDF rendering, localized report text, chart/image composition and the typed
HTTP client used only for report images. Depend on Export contracts and Meals
read-model/enum owners, never on central Infrastructure, DbContext or executable
hosts.

Preserve rendering limits, cancellation, timeouts, image byte limits, DNS/IP
validation and connection pinning. Redirects and proxies remain disabled.
Compose with AddExportInfrastructure and AddExportResources in hosts; localization
implements the Export-owned IDiaryPdfReportTextProvider. Preserve neutral and
Russian resource keys, formatting placeholders, encoding and culture fallback.
No database, retained file or background job ownership.

Focused tests: Modules/Export/tests/FoodDiary.Modules.Export.Infrastructure.Tests.
