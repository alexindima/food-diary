# BodyMetrics Domain

Keep `WeightEntry`, `WaistEntry`, `WeightEntryId`, and `WaistEntryId` here with their existing `FoodDiary.Domain` CLR namespaces. Reference Users Domain.Contracts for `UserId`, DesiredWeightKg and DesiredWaistCm, and the exact module or FoodDiary.Domain.Primitives owner for values and guards. Preserve scalar foreign keys, validation, UTC date normalization, and audit timestamp behavior. Do not add reverse `User.WeightEntries` or `User.WaistEntries` navigations. Weight and waist goals belong to Users Domain.

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.
