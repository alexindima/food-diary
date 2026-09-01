# BodyMetrics Domain

Keep `WeightEntry`, `WaistEntry`, `WeightEntryId`, and `WaistEntryId` here with their existing `FoodDiary.Domain` CLR namespaces. Reference central Domain one-way for `User`, `UserId`, shared value objects, and primitives. Preserve forward EF navigations, validation, UTC date normalization, and audit timestamp behavior. Do not add reverse `User.WeightEntries` or `User.WaistEntries` navigations. Weight and waist goals remain centrally owned.
