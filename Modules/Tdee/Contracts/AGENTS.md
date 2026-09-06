# Tdee consumer contracts

Own TdeeInsightModel, TdeeConfidence and GetTdeeInsightQuery consumed by Dashboard. Keep calculations, repositories and handlers in Application.

Preserve existing CLR namespaces and wire fields during coordinated rebuilds. Do not add persistence, provider clients, DI registration or handlers to this package. Reference the owning contract directly from every consumer.
