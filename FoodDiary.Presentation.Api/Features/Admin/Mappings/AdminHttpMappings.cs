using FoodDiary.Application.Admin.Commands.UpdateAdminUser;
using FoodDiary.Application.Admin.Commands.UpsertAdminAiPrompt;
using FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;
using FoodDiary.Presentation.Api.Features.Admin.Requests;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminHttpMappings {
    public static UpsertAdminEmailTemplateCommand ToCommand(
        this AdminEmailTemplateUpsertHttpRequest request,
        string key,
        string locale) {
        return new UpsertAdminEmailTemplateCommand(
            Key: key,
            Locale: locale,
            Subject: request.Subject,
            HtmlBody: request.HtmlBody,
            TextBody: request.TextBody,
            IsActive: request.IsActive);
    }

    public static UpsertAdminAiPromptCommand ToCommand(
        this AdminAiPromptUpsertHttpRequest request,
        string key,
        string locale) {
        return new UpsertAdminAiPromptCommand(
            Key: key,
            Locale: locale,
            PromptText: request.PromptText,
            IsActive: request.IsActive);
    }

    public static UpdateAdminUserCommand ToCommand(this AdminUserUpdateHttpRequest request, Guid userId) {
        return new UpdateAdminUserCommand(
            UserId: userId,
            IsActive: request.IsActive,
            IsEmailConfirmed: request.IsEmailConfirmed,
            Roles: request.Roles ?? [],
            Language: request.Language,
            AiInputTokenLimit: request.AiInputTokenLimit,
            AiOutputTokenLimit: request.AiOutputTokenLimit);
    }
}
