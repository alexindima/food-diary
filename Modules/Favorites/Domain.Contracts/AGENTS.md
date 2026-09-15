# Favorites scalar contracts

Own stable Favorites IDs. Preserve CLR namespaces, numeric values and ID conversions. Depend only on shared domain primitives. Aggregates, mutable state, events, repositories, EF and application behavior stay outside this project. Actual consumers reference this owner directly.

Use the canonical project name as the namespace root and match folders. Projects are siblings. Public owner use cases are Contracts requests dispatched through ISender; keep outbound source ports and reusable algorithms separate. Preserve authorization, cancellation, wire shapes and persistence semantics.
