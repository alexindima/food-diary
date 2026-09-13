# AI internal application ports

Own provider, quota, job-store and repository ports, internal models and errors.
Keep legacy CLR namespaces. Reference owner Contracts for public DTOs; never
re-export repository/provider capability through Contracts. Administration services
and completed-recognition reads live in Modules/Ai/Contracts.
Implementations remain in AI Application and Infrastructure.
