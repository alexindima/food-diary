using FoodDiary.Modules.Lessons.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Lessons.Contracts.Common;

public sealed record LessonAdministrationItem(
    string Title,
    string Content,
    string? Summary,
    string Locale,
    LessonCategory Category,
    LessonDifficulty Difficulty,
    int EstimatedReadMinutes,
    int SortOrder,
    bool IsPublished = true);
