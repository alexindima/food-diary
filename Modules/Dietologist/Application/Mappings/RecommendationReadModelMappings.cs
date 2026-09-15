using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Dietologist.Application.Models;

namespace FoodDiary.Modules.Dietologist.Application.Mappings;

internal static class RecommendationReadModelMappings {
    public static RecommendationModel ToModel(this RecommendationReadModel recommendation) =>
        new(
            recommendation.RecommendationId,
            recommendation.DietologistUserId,
            recommendation.DietologistFirstName,
            recommendation.DietologistLastName,
            recommendation.Text,
            recommendation.IsRead,
            recommendation.CreatedAtUtc,
            recommendation.ReadAtUtc);
}
