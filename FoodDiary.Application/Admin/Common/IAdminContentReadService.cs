using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Models;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Admin.Common;

public interface IAdminContentReadService {
    Task<IReadOnlyList<AdminLessonModel>> GetLessonsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminEmailTemplateModel>> GetEmailTemplatesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminAiPromptModel>> GetAiPromptsAsync(CancellationToken cancellationToken);

    Task<PagedResponse<AdminContentReportModel>> GetContentReportsAsync(
        ReportStatus? status,
        int page,
        int limit,
        CancellationToken cancellationToken);
}
