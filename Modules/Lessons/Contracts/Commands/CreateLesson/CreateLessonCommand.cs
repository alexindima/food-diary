using FoodDiary.Modules.Lessons.Contracts.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Lessons.Contracts.Commands.CreateLesson;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record CreateLessonCommand(
    string Title,
    string Content,
    string? Summary,
    string Locale,
    LessonCategory Category,
    LessonDifficulty Difficulty,
    int EstimatedReadMinutes,
    int SortOrder,
    bool IsPublished = true) : IRequest<Result<LessonAdminReadModel>>;
