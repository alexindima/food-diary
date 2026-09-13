using FoodDiary.Modules.Admin.Application.Abstractions.Models;

namespace FoodDiary.Application.Admin.Models;

public sealed record AdminDashboardPeriodModel(DateTime FromUtc, DateTime ToUtc,
    AdminDashboardMetrics Metrics, IReadOnlyList<AdminBillingRevenueCurrencyReadModel> Currencies);
