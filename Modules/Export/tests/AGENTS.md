# Export tests

Own application export tests and PDF adapter/network-policy tests. Follow root
Tooling/Testing/AGENTS.md. Keep test-only helpers excluded from coverage, use the shared test
build settings and keep fixtures local. Preserve existing rendering, timeout,
cancellation, image validation, DNS rebinding and loopback-socket cases.

HTTP/Swagger, Resources and host composition tests retain their existing owners.

Use the canonical project name as the namespace root and match folders. Projects are siblings. Public owner use cases are Contracts requests dispatched through ISender; keep outbound source ports and reusable algorithms separate. Preserve authorization, cancellation, wire shapes and persistence semantics.
