using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Lessons.Common;

public static class LessonErrors {
    public static Error NotFound(Guid id) => new(
        "Lesson.NotFound",
        $"Lesson with ID {id} was not found.",
        Kind: ErrorKind.NotFound);
}
