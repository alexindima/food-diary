namespace FoodDiary.Modules.Lessons.Presentation.Responses;

public sealed record LessonDetailHttpResponse(
    Guid Id,
    string Title,
    string Content,
    string? Summary,
    string Category,
    string Difficulty,
    int EstimatedReadMinutes,
    bool IsRead);
