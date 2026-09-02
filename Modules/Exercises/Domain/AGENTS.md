# Exercises Domain

Keep ExerciseEntry, ExerciseEntryId and ExerciseType here with FoodDiary.Domain CLR namespaces. Reference Users Domain for User, Users Domain.Contracts for UserId and central Domain for DomainGuard via IVT. Preserve the EF navigation setter and all validation, UTC/date and rounding behavior.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and residual central Domain only for shared values/guards. Preserve all existing relationships.
