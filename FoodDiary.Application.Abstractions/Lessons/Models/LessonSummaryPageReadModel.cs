namespace FoodDiary.Application.Abstractions.Lessons.Models;

public sealed record LessonSummaryPageReadModel(
    IReadOnlyList<LessonSummaryReadModel> Items,
    int TotalCount,
    int TotalLessonCount);
