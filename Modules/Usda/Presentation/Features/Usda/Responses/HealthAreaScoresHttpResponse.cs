namespace FoodDiary.Presentation.Api.Features.Usda.Responses;

public sealed record HealthAreaScoresHttpResponse(
    HealthAreaScoreHttpResponse Heart,
    HealthAreaScoreHttpResponse Bone,
    HealthAreaScoreHttpResponse Immune,
    HealthAreaScoreHttpResponse Energy,
    HealthAreaScoreHttpResponse Antioxidant);
