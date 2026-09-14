# BodyMetrics consumer contracts

Own weight/waist read requests and their four immutable entry/summary models.
Use FoodDiary.Modules.BodyMetrics.Contracts with WeightEntries and WaistEntries feature namespaces. Preserve user scoping, ordering,
limits, date boundaries and quantization semantics. Depend only on scalar UserId and the shared Mediator. Callers retain their existing access checks and normalized date ranges; these user-scoped composition queries do not infer HTTP identity.
Repository ports and aggregate entities remain internal module dependencies.
