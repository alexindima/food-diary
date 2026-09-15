namespace FoodDiary.Modules.Lessons.Application.Abstractions.Models;

public sealed record LessonSummaryPageReadModel(
    IReadOnlyList<LessonSummaryReadModel> Items,
    int TotalCount,
    int TotalLessonCount,
    IReadOnlyList<string> AvailableCategories);
