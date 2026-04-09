namespace FoodDiary.Application.Usda.Models;

public sealed record HealthAreaScoresModel(
    HealthAreaScoreModel Heart,
    HealthAreaScoreModel Bone,
    HealthAreaScoreModel Immune,
    HealthAreaScoreModel Energy,
    HealthAreaScoreModel Antioxidant);

public sealed record HealthAreaScoreModel(
    int Score,
    string Grade);
