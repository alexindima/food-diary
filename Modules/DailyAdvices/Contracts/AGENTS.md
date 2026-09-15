# DailyAdvices consumer contracts

Own DailyAdviceModel and GetDailyAdviceQuery consumed by Dashboard. Keep generation, persistence and query handlers in Application.

Keep canonical folder namespaces and preserve wire fields during coordinated rebuilds. Do not add persistence, provider clients, DI registration or handlers to this package. Reference the owning contract directly from every consumer.
