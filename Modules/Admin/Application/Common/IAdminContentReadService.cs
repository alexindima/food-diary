using FoodDiary.Application.ContentReports.Models;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Admin.Common;

public interface IAdminContentReadService {
    Task<IReadOnlyList<AdminTemplateRevisionModel>> GetTemplateRevisionsAsync(string key, string locale, bool isAiPrompt, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminLessonModel>> GetLessonsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminEmailTemplateModel>> GetEmailTemplatesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminAiPromptModel>> GetAiPromptsAsync(CancellationToken cancellationToken);

    Task<PagedResponse<AdminContentReportModel>> GetContentReportsAsync(
        ReportStatus? status,
        int page,
        int limit,
        CancellationToken cancellationToken, ContentReportAdminFilter? filter = null);
}
