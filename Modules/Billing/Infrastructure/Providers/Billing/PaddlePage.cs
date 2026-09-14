namespace FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;

internal sealed record PaddlePage<T>(IReadOnlyList<T> Items, string? Next);
