# BodyMetrics Domain

Keep `WeightEntry`, `WaistEntry`, `WeightEntryId`, and `WaistEntryId` here with their existing `FoodDiary.Domain` CLR namespaces. Reference Users Domain for `User`, Users Domain.Contracts for `UserId`, and the exact module or FoodDiary.Domain.Primitives owner for values and guards. Preserve forward EF navigations, validation, UTC date normalization, and audit timestamp behavior. Do not add reverse `User.WeightEntries` or `User.WaistEntries` navigations. Weight and waist goals belong to Users Domain.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and the exact module or Primitives owner for shared values/guards. Preserve all existing relationships.
