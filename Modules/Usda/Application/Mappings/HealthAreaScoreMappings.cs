using FoodDiary.Modules.Usda.Contracts.Models;
using FoodDiary.Modules.Usda.Domain.ValueObjects;

namespace FoodDiary.Modules.Usda.Application.Mappings;

public static class HealthAreaScoreMappings {
    public static HealthAreaScoresModel ToModel(this HealthAreaScores scores) =>
        new(scores.Heart.ToModel(),
            scores.Bone.ToModel(),
            scores.Immune.ToModel(),
            scores.Energy.ToModel(),
            scores.Antioxidant.ToModel());

    private static HealthAreaScoreModel ToModel(this HealthAreaScore score) =>
        new(score.Score, score.Grade.ToString().ToLowerInvariant());
}
