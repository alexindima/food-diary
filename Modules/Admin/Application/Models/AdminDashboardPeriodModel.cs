using FoodDiary.Application.Abstractions.Admin.Models;

namespace FoodDiary.Application.Admin.Models;

public sealed record AdminDashboardPeriodModel(DateTime FromUtc, DateTime ToUtc,
    AdminDashboardMetrics Metrics, IReadOnlyList<AdminBillingRevenueCurrencyReadModel> Currencies);
