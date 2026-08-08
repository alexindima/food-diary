namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AdminBillingRevenueSummaryReadModel(
    DateTime FromUtc,
    DateTime ToUtc,
    IReadOnlyList<AdminBillingRevenueCurrencyReadModel> Currencies);
