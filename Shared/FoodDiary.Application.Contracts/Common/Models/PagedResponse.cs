namespace FoodDiary.Application.Abstractions.Common.Models;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Data,
    int Page,
    int Limit,
    int TotalPages,
    int TotalItems);
