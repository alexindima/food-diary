using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.ContentReports.Models;
using FoodDiary.Modules.Lessons.Contracts.Models;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.ContentReports.Common;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Modules.Lessons.Contracts.Common;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Admin.Services;

public sealed class AdminContentReadService(
    ILessonAdministrationReadService lessonReadService,
    IEmailTemplateAdministrationReadService emailTemplateReadService,
    IAiAdministrationReadService aiReadService,
    IContentReportAdministrationReadService contentReportReadService)
    : IAdminContentReadService {
    public async Task<IReadOnlyList<AdminTemplateRevisionModel>> GetTemplateRevisionsAsync(string key, string locale, bool isAiPrompt, CancellationToken cancellationToken) {
        if (isAiPrompt) {
            IReadOnlyList<AiPromptRevisionReadModel> revisions = await aiReadService.GetPromptRevisionsAsync(key, locale, cancellationToken).ConfigureAwait(false);
            return revisions.Select(item => new AdminTemplateRevisionModel(item.Id, Subject: null, HtmlBody: null, item.PromptText,
                item.IsActive, item.Version, item.SavedOnUtc, item.ArchivedOnUtc)).ToList();
        }
        IReadOnlyList<EmailTemplateRevisionReadModel> emails = await emailTemplateReadService.GetRevisionsAsync(key, locale, cancellationToken).ConfigureAwait(false);
        return emails.Select(item => new AdminTemplateRevisionModel(item.Id, item.Subject, item.HtmlBody, item.TextBody,
            item.IsActive, Version: null, item.SavedOnUtc, item.ArchivedOnUtc)).ToList();
    }

    public async Task<IReadOnlyList<AdminLessonModel>> GetLessonsAsync(CancellationToken cancellationToken) {
        IReadOnlyList<LessonAdminReadModel> lessons = await lessonReadService
            .GetLessonsAsync(cancellationToken)
            .ConfigureAwait(false);
        return lessons.Select(static lesson => lesson.ToAdminModel()).ToList();
    }

    public async Task<IReadOnlyList<AdminEmailTemplateModel>> GetEmailTemplatesAsync(CancellationToken cancellationToken) {
        IReadOnlyList<EmailTemplateReadModel> templates = await emailTemplateReadService
            .GetTemplatesAsync(cancellationToken)
            .ConfigureAwait(false);
        return templates.Select(static template => template.ToAdminModel()).ToList();
    }

    public async Task<IReadOnlyList<AdminAiPromptModel>> GetAiPromptsAsync(CancellationToken cancellationToken) {
        IReadOnlyList<AiPromptTemplateReadModel> templates = await aiReadService
            .GetPromptTemplatesAsync(cancellationToken)
            .ConfigureAwait(false);
        return templates.Select(static template => template.ToAdminModel()).ToList();
    }

    public async Task<PagedResponse<AdminContentReportModel>> GetContentReportsAsync(
        ReportStatus? status,
        int page,
        int limit,
        CancellationToken cancellationToken, ContentReportAdminFilter? filter = null) {
        (IReadOnlyList<ContentReportAdminReadModel> items, int total) = await contentReportReadService
            .GetReportsAsync(status, page, limit, cancellationToken, filter)
            .ConfigureAwait(false);

        IReadOnlyList<AdminContentReportModel> models = [
            .. items.Select(static report => report.ToAdminModel()),
        ];

        int totalPages = (int)Math.Ceiling(total / (double)limit);
        return new PagedResponse<AdminContentReportModel>(models, page, limit, totalPages, total);
    }
}
