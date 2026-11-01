namespace FoodDiary.Modules.Billing.Presentation.Responses;

public sealed record CancelSubscriptionHttpResponse(Guid CancellationId, BillingOverviewHttpResponse Overview);
