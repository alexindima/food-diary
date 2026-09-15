namespace FoodDiary.Modules.Usda.Presentation.Responses;

public sealed record HealthAreaScoresHttpResponse(
    HealthAreaScoreHttpResponse Heart,
    HealthAreaScoreHttpResponse Bone,
    HealthAreaScoreHttpResponse Immune,
    HealthAreaScoreHttpResponse Energy,
    HealthAreaScoreHttpResponse Antioxidant);
