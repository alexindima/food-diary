namespace FoodDiary.Presentation.Api.Features.Fasting.Responses;

public sealed record FastingInsightsHttpResponse(
    IReadOnlyList<FastingMessageHttpResponse> Alerts,
    IReadOnlyList<FastingMessageHttpResponse> Insights);
