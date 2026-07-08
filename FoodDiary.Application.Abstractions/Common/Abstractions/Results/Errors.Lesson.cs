using FoodDiary.Results;

using FoodDiary.Application.Abstractions.Lessons.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Lesson {
        public static Error NotFound(Guid id) => LessonErrors.NotFound(id);
    }
}
