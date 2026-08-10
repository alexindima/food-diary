namespace FoodDiary.Presentation.Api.Features.Lessons.Responses;

public sealed record LessonPageHttpResponse(
    IReadOnlyList<LessonSummaryHttpResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    int TotalLessonCount,
    int ReadLessonCount);
