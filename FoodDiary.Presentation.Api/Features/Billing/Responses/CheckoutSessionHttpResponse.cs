namespace FoodDiary.Presentation.Api.Features.Billing.Responses;

public sealed record CheckoutSessionHttpResponse(
    string SessionId,
    string Url,
    string Plan);
