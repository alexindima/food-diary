using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Admin.Mappings;

public static class AdminAiPromptMappings {
    public static AdminAiPromptModel ToAdminModel(this AiPromptTemplateReadModel template) =>
        new(
            template.Id,
            template.Key,
            template.Locale,
            template.PromptText,
            template.Version,
            template.IsActive,
            template.CreatedOnUtc,
            template.ModifiedOnUtc);
}
