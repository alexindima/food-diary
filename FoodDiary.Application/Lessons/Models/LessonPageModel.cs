namespace FoodDiary.Application.Lessons.Models;

public sealed record LessonPageModel(
    IReadOnlyList<LessonSummaryModel> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    int TotalLessonCount,
    int ReadLessonCount);
