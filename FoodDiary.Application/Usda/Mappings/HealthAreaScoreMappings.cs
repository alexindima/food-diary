using FoodDiary.Application.Usda.Models;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Usda.Mappings;

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
