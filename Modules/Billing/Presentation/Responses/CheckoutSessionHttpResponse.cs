namespace FoodDiary.Modules.Billing.Presentation.Responses;

public sealed record CheckoutSessionHttpResponse(
    string SessionId,
    string Url,
    string Plan);
