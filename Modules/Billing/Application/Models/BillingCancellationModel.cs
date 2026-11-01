namespace FoodDiary.Modules.Billing.Application.Models;

public sealed record BillingCancellationModel(Guid CancellationId, BillingOverviewModel Overview);
