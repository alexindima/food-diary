namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Exercise {
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
}
