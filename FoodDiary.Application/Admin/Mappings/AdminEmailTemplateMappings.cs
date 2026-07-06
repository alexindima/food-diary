using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Domain.Entities.Content;

namespace FoodDiary.Application.Admin.Mappings;

public static class AdminEmailTemplateMappings {
    public static AdminEmailTemplateModel ToAdminModel(this EmailTemplate template) =>
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
