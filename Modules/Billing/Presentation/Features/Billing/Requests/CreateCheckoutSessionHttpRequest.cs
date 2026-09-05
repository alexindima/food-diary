namespace FoodDiary.Presentation.Api.Features.Billing.Requests;

public sealed record CreateCheckoutSessionHttpRequest(string Plan, string? Provider);
