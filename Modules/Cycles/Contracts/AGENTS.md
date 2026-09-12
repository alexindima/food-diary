# Cycles consumer contracts

Own the cycle read DTOs, ICycleReadService and GetCurrentCycleQuery consumed by Dashboard and Export. Keep handlers, repositories and mutation policy in Application. The eleven public cycle enums belong to dependency-free Cycles Domain.Contracts; do not expose aggregate instances through these contracts.

Preserve existing CLR namespaces and wire fields during coordinated rebuilds. Do not add persistence, provider clients, DI registration or handlers to this package. Reference the owning contract directly from every consumer.

Consumer Contracts must not reference Cycles Domain. Preserve enum values and wire fields.
