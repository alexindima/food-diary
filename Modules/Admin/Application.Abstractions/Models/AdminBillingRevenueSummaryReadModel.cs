namespace FoodDiary.Modules.Admin.Application.Abstractions.Models;

public sealed record AdminBillingRevenueSummaryReadModel(
    DateTime FromUtc,
    DateTime ToUtc,
    IReadOnlyList<AdminBillingRevenueCurrencyReadModel> Currencies,
    int RenewalPaymentRecords = 0,
    int ScheduledCancellations = 0);
