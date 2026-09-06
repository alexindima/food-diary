# Cycles consumer contracts

Own the cycle read DTOs, ICycleReadService and GetCurrentCycleQuery consumed by Dashboard and Export. Keep handlers, repositories and mutation policy in Application. Existing cycle enums remain in Cycles Domain; do not expose aggregate instances through these contracts.

Preserve existing CLR namespaces and wire fields during coordinated rebuilds. Do not add persistence, provider clients, DI registration or handlers to this package. Reference the owning contract directly from every consumer.
