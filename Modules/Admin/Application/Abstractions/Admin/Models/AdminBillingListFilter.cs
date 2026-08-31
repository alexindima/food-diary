namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AdminBillingListFilter(
    int Page,
    int Limit,
    string? Provider,
    string? Status,
    string? Kind,
    string? Search,
    DateTime? FromUtc,
    DateTime? ToUtc);
