using FoodDiary.Application.Admin.Commands.UpdateAdminUser;
using FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;
using FoodDiary.Presentation.Api.Features.Admin.Requests;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminHttpMappings {
    public static UpsertAdminEmailTemplateCommand ToCommand(
        this AdminEmailTemplateUpsertHttpRequest request,
        string key,
        string locale) {
        return new UpsertAdminEmailTemplateCommand(
            key,
            locale,
            request.Subject,
            request.HtmlBody,
            request.TextBody,
            request.IsActive);
    }

    public static UpdateAdminUserCommand ToCommand(this AdminUserUpdateHttpRequest request, Guid userId) {
        return new UpdateAdminUserCommand(
            userId,
            request.IsActive,
            request.IsEmailConfirmed,
            request.Roles ?? [],
            request.Language,
            request.AiInputTokenLimit,
            request.AiOutputTokenLimit);
    }
}
