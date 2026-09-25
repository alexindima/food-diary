# AI internal application ports

Own provider, quota, job-store and repository ports, internal models and errors.
Use project-and-folder namespaces. Reference owner Contracts for public DTOs; never
re-export repository/provider capability through Contracts. Administration services
and completed-recognition reads live in Modules/Ai/Contracts.
Implementations remain in AI Application and Infrastructure.

IAiUsageQuery is the internal immutable usage-reporting port implemented in host ReadModel.Composition; it does not expose aggregate writes.

Prompt persistence exposes only IAiPromptTemplateReadModelRepository and IAiPromptTemplateWriteRepository. Do not restore an unused aggregate read port or a composite alias solely for DI.

Recognition queries, completed-result readers and notifications consume IFoodRecognitionJobReader. IFoodRecognitionJobStore owns creation, completed-job deletion and worker mutations; do not expose a combined read/write port.

Prompt writes load the tracked aggregate once through GetByKeyAsync. Do not add tracking switches or a second ID lookup. PostgreSQL xmin protects concurrent text and activation changes.
