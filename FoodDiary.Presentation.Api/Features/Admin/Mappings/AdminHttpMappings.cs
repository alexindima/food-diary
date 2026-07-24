using FoodDiary.Application.Admin.Commands.CreateAdminLesson;
using FoodDiary.Application.Admin.Commands.CreateAdminUser;
using FoodDiary.Application.Admin.Commands.DeleteAdminLesson;
using FoodDiary.Application.Admin.Commands.DismissContentReport;
using FoodDiary.Application.Admin.Commands.ImportAdminLessons;
using FoodDiary.Application.Admin.Commands.MarkAdminMailInboxMessageRead;
using FoodDiary.Application.Admin.Commands.ReviewContentReport;
using FoodDiary.Application.Admin.Commands.SendAdminEmailTemplateTest;
using FoodDiary.Application.Admin.Commands.SetAdminUserPassword;
using FoodDiary.Application.Admin.Commands.StartAdminImpersonation;
using FoodDiary.Application.Admin.Commands.UpdateAdminLesson;
using FoodDiary.Application.Admin.Commands.UpdateAdminUser;
using FoodDiary.Application.Admin.Commands.UpsertAdminAiPrompt;
using FoodDiary.Application.Admin.Commands.UpsertAdminEmailTemplate;
using FoodDiary.Application.Admin.Queries.GetAdminImpersonationSessions;
using FoodDiary.Application.Admin.Queries.GetCollaborationAudit;
using FoodDiary.Presentation.Api.Features.Admin.Requests;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminHttpMappings {
    public static GetCollaborationAuditQuery ToQuery(this GetCollaborationAuditHttpQuery query) =>
        new(query.ClientUserId, query.Limit);

    public static CreateAdminUserCommand ToCommand(
        this AdminUserCreateHttpRequest request,
        Guid actorUserId,
        string? clientOrigin) {
        return new CreateAdminUserCommand(
            Email: request.Email,
            FirstName: request.FirstName,
            LastName: request.LastName,
            Language: request.Language,
            Roles: request.Roles,
            TemporaryPassword: request.TemporaryPassword,
            GeneratePassword: request.GeneratePassword,
            IsEmailConfirmed: request.IsEmailConfirmed,
            SendCredentialsEmail: request.SendCredentialsEmail,
            RequirePasswordChange: request.RequirePasswordChange,
            ClientOrigin: clientOrigin,
            ActorUserId: actorUserId);
    }

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

    public static UpdateAdminUserCommand ToCommand(this AdminUserUpdateHttpRequest request, Guid userId, Guid actorUserId) {
        return new UpdateAdminUserCommand(
            UserId: userId,
            IsActive: request.IsActive,
            IsEmailConfirmed: request.IsEmailConfirmed,
            Roles: request.Roles ?? [],
            Language: request.Language,
            AiInputTokenLimit: request.AiInputTokenLimit,
            AiOutputTokenLimit: request.AiOutputTokenLimit,
            ActorUserId: actorUserId);
    }

    public static SetAdminUserPasswordCommand ToCommand(this AdminUserSetPasswordHttpRequest request, Guid userId) {
        return new SetAdminUserPasswordCommand(
            UserId: userId,
            NewPassword: request.NewPassword);
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

    public static ImportAdminLessonsCommand ToImportCommand(this AdminLessonsImportHttpRequest request) {
        return new ImportAdminLessonsCommand(
            Version: request.Version,
            Lessons: request.Lessons.Select(static lesson => new ImportAdminLessonItem(
                Title: lesson.Title,
                Content: lesson.Content,
                Summary: lesson.Summary,
                Locale: lesson.Locale,
                Category: lesson.Category,
                Difficulty: lesson.Difficulty,
                EstimatedReadMinutes: lesson.EstimatedReadMinutes,
                SortOrder: lesson.SortOrder)).ToList());
    }

    public static DeleteAdminLessonCommand ToDeleteCommand(this Guid id) {
        return new DeleteAdminLessonCommand(id);
    }

    public static MarkAdminMailInboxMessageReadCommand ToMarkMailInboxMessageReadCommand(this Guid id) {
        return new MarkAdminMailInboxMessageReadCommand(id);
    }
}
