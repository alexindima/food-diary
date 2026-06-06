namespace FoodDiary.Presentation.Api.Features.Gamification.Responses;

public sealed record BadgeHttpResponse(
    string Key,
    string Category,
    int Threshold,
    bool IsEarned);
