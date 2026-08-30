using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Dietologist.Common;

internal static class RecommendationTemplateMapping {
    public static RecommendationTemplateModel ToModel(this RecommendationTemplate template) =>
        new(
            template.Id.Value,
            template.Name,
            template.Text,
            template.IsArchived,
            template.CreatedOnUtc,
            template.ModifiedOnUtc);

    public static RecommendationTemplateModel ToModel(this RecommendationTemplateReadModel template) =>
        new(
            template.Id,
            template.Name,
            template.Text,
            template.IsArchived,
            template.CreatedAtUtc,
            template.ModifiedAtUtc);
}
