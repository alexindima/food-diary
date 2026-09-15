# Final module-to-central infrastructure dependencies

Ai quota orphan telemetry belongs to `Modules/Ai/Infrastructure/Diagnostics`.
It retains the `FoodDiary.Infrastructure` meter, the
`fooddiary.ai.quota_orphans` counter and positive-count-only recording. Provider
request telemetry and quota transaction behavior are unchanged.

Identity uses the existing shared Authentication.Contracts assembly for
`Options/JwtOptions`. Its namespace follows that project's existing root:
`FoodDiary.Application.Abstractions.Options`. Central composition still binds and
validates the same `Jwt` configuration section; Identity token generation and API
bearer validation consume the same type. No credential value, token format,
validation rule or expiry default changes.

Identity infrastructure explicitly references the existing HTTP and Data
Protection packages. It has no reference to central Infrastructure. Internal ports
and persistence mappings live in sibling `Application.Abstractions` and
`PersistenceModel` projects. Those projects and Infrastructure use namespaces
matching their project names and folders, including provider implementations.
Application, Domain and presentation CLR names are outside this relocation.
Logger categories derived from moved provider CLR names follow the new namespace.

Central migrations and transaction coordination remain central. Current snapshot
metadata follows the relocated technical Identity types; historical migrations
retain their original metadata. Table, column, index, relationship and concurrency
configuration bodies are unchanged. Deploy the affected backend assemblies
together; no new configuration keys or database migration are intended.

Architecture guards reject transitive central Infrastructure dependencies for both
Ai and Identity, enforce the three canonical Identity project namespaces and remove
the two resolved nesting exceptions. The remaining central composition layer
continues to serve hosts through narrow shared persistence contracts.
