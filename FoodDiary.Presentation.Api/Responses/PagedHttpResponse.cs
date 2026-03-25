namespace FoodDiary.Presentation.Api.Responses;

public sealed record PagedHttpResponse<T>(
    IReadOnlyList<T> Data,
    int Page,
    int Limit,
    int TotalPages,
    int TotalItems);
