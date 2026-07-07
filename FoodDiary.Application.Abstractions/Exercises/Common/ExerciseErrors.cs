using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Abstractions.Exercises.Common;

public static class ExerciseErrors {
    public static Error NotFound(Guid id) => new(
        "Exercise.NotFound",
        $"Exercise entry with ID {id} was not found.",
        Kind: ErrorKind.NotFound);

    public static Error InvalidDuration => new(
        "Exercise.InvalidDuration",
        "Exercise duration must be positive.",
        Kind: ErrorKind.Validation);

    public static Error InvalidCalories => new(
        "Exercise.InvalidCalories",
        "Calories burned must be non-negative.",
        Kind: ErrorKind.Validation);
}
