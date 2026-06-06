namespace FoodDiary.Presentation.Api.Features.Usda.Responses;

public sealed record HealthAreaScoreHttpResponse(
    int Score,
    string Grade);
