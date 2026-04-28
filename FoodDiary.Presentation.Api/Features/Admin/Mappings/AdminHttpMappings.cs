using FoodDiary.Application.Admin.Commands.CreateAdminLesson;
using FoodDiary.Application.Admin.Commands.DeleteAdminLesson;
using FoodDiary.Application.Admin.Commands.DismissContentReport;
using FoodDiary.Application.Admin.Commands.ReviewContentReport;
using FoodDiary.Application.Admin.Commands.SendAdminEmailTemplateTest;
using FoodDiary.Application.Admin.Commands.StartAdminImpersonation;
using FoodDiary.Application.Admin.Commands.UpdateAdminLesson;
using FoodDiary.Application.Admin.Commands.UpdateAdminUser;
using FoodDiary.Application.Admin.Commands.UpsertAdminAiPrompt;
using FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;
using FoodDiary.Application.Admin.Queries.GetAdminImpersonationSessions;
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

    public static SendAdminEmailTemplateTestCommand ToCommand(this AdminEmailTemplateTestHttpRequest request) {
        return new SendAdminEmailTemplateTestCommand(
            ToEmail: request.ToEmail,
            Key: request.Key,
            Subject: request.Subject,
            HtmlBody: request.HtmlBody,
            TextBody: request.TextBody);
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

    public static StartAdminImpersonationCommand ToCommand(
        this AdminImpersonationStartHttpRequest request,
        Guid actorUserId,
        Guid targetUserId,
        string? actorIpAddress,
        string? actorUserAgent) {
        return new StartAdminImpersonationCommand(
            ActorUserId: actorUserId,
            TargetUserId: targetUserId,
            Reason: request.Reason,
            ActorIpAddress: actorIpAddress,
            ActorUserAgent: actorUserAgent);
    }

    public static GetAdminImpersonationSessionsQuery ToQuery(this GetAdminImpersonationSessionsHttpQuery query) {
        return new GetAdminImpersonationSessionsQuery(
            Page: query.Page,
            Limit: query.Limit,
            Search: query.Search);
    }

    public static ReviewContentReportCommand ToReviewCommand(this AdminReportActionHttpRequest request, Guid reportId) {
        return new ReviewContentReportCommand(reportId, request.AdminNote);
    }

    public static DismissContentReportCommand ToDismissCommand(this AdminReportActionHttpRequest request, Guid reportId) {
        return new DismissContentReportCommand(reportId, request.AdminNote);
    }

    public static CreateAdminLessonCommand ToCreateCommand(this AdminLessonCreateHttpRequest request) {
        return new CreateAdminLessonCommand(
            Title: request.Title,
            Content: request.Content,
            Summary: request.Summary,
            Locale: request.Locale,
            Category: request.Category,
            Difficulty: request.Difficulty,
            EstimatedReadMinutes: request.EstimatedReadMinutes,
            SortOrder: request.SortOrder);
    }

    public static UpdateAdminLessonCommand ToUpdateCommand(this AdminLessonUpdateHttpRequest request, Guid id) {
        return new UpdateAdminLessonCommand(
            Id: id,
            Title: request.Title,
            Content: request.Content,
            Summary: request.Summary,
            Locale: request.Locale,
            Category: request.Category,
            Difficulty: request.Difficulty,
            EstimatedReadMinutes: request.EstimatedReadMinutes,
            SortOrder: request.SortOrder);
    }

    public static DeleteAdminLessonCommand ToDeleteCommand(this Guid id) {
        return new DeleteAdminLessonCommand(id);
    }
}
