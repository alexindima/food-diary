using FoodDiary.Modules.Dietologist.Application.Abstractions.Models;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Modules.Dietologist.Domain.Entities;

namespace FoodDiary.Modules.Dietologist.Application.Common;

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
