namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Lesson {
        public static Error NotFound(Guid id) => new(
            "Lesson.NotFound",
            $"Lesson with ID {id} was not found.",
            Kind: ErrorKind.NotFound);
    }
}
