namespace FoodDiary.Modules.Usda.Contracts.Models;

public sealed record HealthAreaScoresModel(
    HealthAreaScoreModel Heart,
    HealthAreaScoreModel Bone,
    HealthAreaScoreModel Immune,
    HealthAreaScoreModel Energy,
    HealthAreaScoreModel Antioxidant);
