namespace FoodDiary.Modules.Usda.Presentation.Responses;

public sealed record HealthAreaScoreHttpResponse(
    int Score,
    string Grade);
