namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminBillingRevenueSummaryHttpResponse(
    DateTime FromUtc,
    DateTime ToUtc,
    IReadOnlyList<AdminBillingRevenueCurrencyHttpResponse> Currencies,
    int RenewalPaymentRecords = 0,
    int ScheduledCancellations = 0);
