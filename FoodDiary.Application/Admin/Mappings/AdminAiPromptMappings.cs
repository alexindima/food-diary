using FoodDiary.Application.Admin.Models;
using FoodDiary.Domain.Entities.Ai;

namespace FoodDiary.Application.Admin.Mappings;

public static class AdminAiPromptMappings {
    public static AdminAiPromptModel ToAdminModel(this AiPromptTemplate template) =>
        new(
            template.Id.Value,
            template.Key,
            template.Locale,
            template.PromptText,
            template.Version,
            template.IsActive,
            template.CreatedOnUtc,
            template.ModifiedOnUtc);
}
