using FoodDiary.Application.Abstractions.Exercises.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Exercise {
        public static Error NotFound(Guid id) => ExerciseErrors.NotFound(id);

        public static Error NotAccessible(Guid id) => ExerciseErrors.NotAccessible(id);

        public static Error InvalidDuration => ExerciseErrors.InvalidDuration;

        public static Error InvalidCalories => ExerciseErrors.InvalidCalories;
    }
}
