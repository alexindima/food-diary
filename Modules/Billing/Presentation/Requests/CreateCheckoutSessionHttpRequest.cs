namespace FoodDiary.Modules.Billing.Presentation.Requests;

public sealed record CreateCheckoutSessionHttpRequest(string Plan, string? Provider);
