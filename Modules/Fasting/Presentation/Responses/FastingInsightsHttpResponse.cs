namespace FoodDiary.Modules.Fasting.Presentation.Responses;

public sealed record FastingInsightsHttpResponse(
    IReadOnlyList<FastingMessageHttpResponse> Alerts,
    IReadOnlyList<FastingMessageHttpResponse> Insights);
