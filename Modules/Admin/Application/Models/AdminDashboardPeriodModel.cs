using FoodDiary.Modules.Admin.Application.Abstractions.Models;

namespace FoodDiary.Modules.Admin.Application.Models;

public sealed record AdminDashboardPeriodModel(DateTime FromUtc, DateTime ToUtc,
    AdminDashboardMetrics Metrics, IReadOnlyList<AdminBillingRevenueCurrencyReadModel> Currencies);
