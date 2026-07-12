using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.ContentReports.Models;
using FoodDiary.Application.Abstractions.Lessons.Models;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.ContentReports.Common;
using FoodDiary.Application.Email.Common;
using FoodDiary.Application.Lessons.Common;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Admin.Services;

public sealed class AdminContentReadService(
    ILessonAdministrationReadService lessonReadService,
    IEmailTemplateAdministrationReadService emailTemplateReadService,
    IAiAdministrationReadService aiReadService,
    IContentReportAdministrationReadService contentReportReadService)
    : IAdminContentReadService {
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
        CancellationToken cancellationToken) {
        (IReadOnlyList<ContentReportAdminReadModel> items, int total) = await contentReportReadService
            .GetReportsAsync(status, page, limit, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<AdminContentReportModel> models = [
            .. items.Select(static report => report.ToAdminModel()),
        ];

        int totalPages = (int)Math.Ceiling(total / (double)limit);
        return new PagedResponse<AdminContentReportModel>(models, page, limit, totalPages, total);
    }
}
