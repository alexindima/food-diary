# Exercises Domain

Keep ExerciseEntry, ExerciseEntryId and ExerciseType here with FoodDiary.Domain CLR namespaces. Reference Users Domain for User, Users Domain.Contracts for UserId and shared Primitives for DomainGuard. Preserve the EF navigation setter and all validation, UTC/date and rounding behavior.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and the exact module or Primitives owner for shared values. Preserve all existing relationships.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access. This domain has no central Domain dependency.
