namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminBillingRevenueCurrencyHttpResponse(
    string Currency,
    decimal Gross,
    decimal Refunds,
    decimal Chargebacks,
    decimal Reversals,
    decimal Net,
    int SuccessfulPayments,
    decimal Tax,
    decimal PaddleFees,
    decimal PaddleEarnings,
    int EarningsTrackedPayments);
