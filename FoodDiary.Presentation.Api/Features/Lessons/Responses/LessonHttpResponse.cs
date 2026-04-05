namespace FoodDiary.Presentation.Api.Features.Lessons.Responses;

public sealed record LessonSummaryHttpResponse(
    Guid Id,
    string Title,
    string? Summary,
    string Category,
    string Difficulty,
    int EstimatedReadMinutes,
    bool IsRead);

public sealed record LessonDetailHttpResponse(
    Guid Id,
    string Title,
    string Content,
    string? Summary,
    string Category,
    string Difficulty,
    int EstimatedReadMinutes,
    bool IsRead);
