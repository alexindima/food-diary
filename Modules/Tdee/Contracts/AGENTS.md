# Tdee consumer contracts

Own TdeeInsightModel, TdeeConfidence and GetTdeeInsightQuery consumed by Dashboard. Keep calculations, repositories and handlers in Application.

Use canonical folder namespaces; preserve wire fields during coordinated rebuilds. Do not add persistence, provider clients, DI registration or handlers to this package. Reference the owning contract directly from every consumer.

Current module convention: all projects use `FoodDiary.Modules.Tdee.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
