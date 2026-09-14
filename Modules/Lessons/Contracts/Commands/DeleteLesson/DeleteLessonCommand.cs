using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Lessons.Contracts.Commands.DeleteLesson;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record DeleteLessonCommand(
    NutritionLessonId LessonId) : IRequest<Result>;
