namespace FoodDiary.Contracts.Common;

public record PagedResponse<T>(
    IReadOnlyList<T> Data,
    int Page,
    int Limit,
    int TotalPages,
    int TotalItems);
