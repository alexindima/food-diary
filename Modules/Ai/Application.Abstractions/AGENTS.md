# AI internal application ports

Own provider, quota, job-store and repository ports, internal models and errors.
Use project-and-folder namespaces. Reference owner Contracts for public DTOs; never
re-export repository/provider capability through Contracts. Administration services
and completed-recognition reads live in Modules/Ai/Contracts.
Implementations remain in AI Application and Infrastructure.

IAiUsageQuery is the internal immutable usage-reporting port implemented in host ReadModel.Composition; it does not expose aggregate writes.
