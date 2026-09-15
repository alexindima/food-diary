# Reusable presentation contracts

Own immutable wire DTOs with preserved CLR names and JSON shapes. Reference only
other Presentation.Contracts required by composite responses. No mappings, methods,
application models, controllers, framework packages, providers or persistence.

Use the canonical project name as the namespace root and match folders. Projects are siblings. Public owner use cases are Contracts requests dispatched through ISender; keep outbound source ports and reusable algorithms separate. Preserve authorization, cancellation, wire shapes and persistence semantics.
