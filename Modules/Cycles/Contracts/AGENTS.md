# Cycles consumer contracts

Own CycleErrors with unchanged error codes, messages and kinds; consumers must
not reference internal Abstractions merely for error factories.

Own the cycle read DTOs, GetCurrentCycleQuery consumed by Dashboard and Export. Keep handlers, repositories and mutation policy in Application. The eleven public cycle enums belong to dependency-free Cycles Domain.Contracts; do not expose aggregate instances through these contracts.

Use canonical project-relative namespaces and preserve wire fields during coordinated rebuilds. Do not add persistence, provider clients, DI registration or handlers to this package. Reference the owning contract directly from every consumer.

Consumer Contracts must not reference Cycles Domain. Preserve enum values and wire fields.
