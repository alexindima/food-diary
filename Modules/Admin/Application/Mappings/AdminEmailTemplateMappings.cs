using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Modules.Admin.Application.Models;

namespace FoodDiary.Modules.Admin.Application.Mappings;

public static class AdminEmailTemplateMappings {
    public static AdminEmailTemplateModel ToAdminModel(this EmailTemplateReadModel template) =>
        new(
            template.Id,
            template.Key,
            template.Locale,
            template.Subject,
            template.HtmlBody,
            template.TextBody,
            template.IsActive,
            template.CreatedOnUtc,
            template.ModifiedOnUtc);
}
