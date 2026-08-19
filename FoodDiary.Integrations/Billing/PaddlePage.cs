namespace FoodDiary.Integrations.Billing;

internal sealed record PaddlePage<T>(IReadOnlyList<T> Items, string? Next);
