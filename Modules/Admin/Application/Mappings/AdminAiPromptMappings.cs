using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Modules.Admin.Application.Models;

namespace FoodDiary.Modules.Admin.Application.Mappings;

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
