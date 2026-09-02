# Export tests

Own application export tests and PDF adapter/network-policy tests. Follow root
tests/AGENTS.md. Keep test-only helpers excluded from coverage, use the shared test
build settings and keep fixtures local. Preserve existing rendering, timeout,
cancellation, image validation, DNS rebinding and loopback-socket cases.

HTTP/Swagger, Resources and host composition tests retain their existing owners.
