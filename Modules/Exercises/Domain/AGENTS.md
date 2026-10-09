# Exercises Domain

Keep ExerciseEntry, ExerciseEntryId and ExerciseType here with folder-aligned module namespaces. Reference Users Domain.Contracts for UserId and shared Primitives for DomainGuard. Preserve scalar UserId and all validation, UTC/date and rounding behavior.

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access. This domain has no central Domain dependency.

Use the canonical project name as the namespace root and match folders. Projects are siblings. Public owner use cases are Contracts requests dispatched through ISender; keep outbound source ports and reusable algorithms separate. Preserve authorization, cancellation, wire shapes and persistence semantics.

Application mutations use CreateWithValues/UpdateValues with distinct ExerciseDay, ExerciseDuration and BurnedEnergy. Keep 1..1440 whole minutes, 0..10000 finite kilocalories and one-decimal ToEven normalization. ExerciseDay preserves the existing Unspecified calendar date and Local-to-UTC date extraction. Scalar storage and compatibility Create/Update retain the current behavior; do not equate minutes with calories or a recorded day with an instant.
