namespace FoodDiary.Modules.Lessons.Presentation.Responses;

public sealed record LessonSummaryHttpResponse(
    Guid Id,
    string Title,
    string? Summary,
    string Category,
    string Difficulty,
    int EstimatedReadMinutes,
    bool IsRead);
