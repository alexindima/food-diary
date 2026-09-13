namespace FoodDiary.Modules.Admin.Application.Abstractions.Models;

public sealed record AdminBillingRevenueCurrencyReadModel(
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
