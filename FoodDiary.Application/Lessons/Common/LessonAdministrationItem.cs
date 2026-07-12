using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Lessons.Common;

public sealed record LessonAdministrationItem(
    string Title,
    string Content,
    string? Summary,
    string Locale,
    LessonCategory Category,
    LessonDifficulty Difficulty,
    int EstimatedReadMinutes,
    int SortOrder);
