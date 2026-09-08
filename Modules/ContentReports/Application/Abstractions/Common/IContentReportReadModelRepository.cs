using FoodDiary.Application.ContentReports.Models;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.ContentReports.Common;

public interface IContentReportReadModelRepository {
    Task<(IReadOnlyList<ContentReportAdminReadModel> Items, int Total)> GetPagedAdminReadModelsAsync(
        ReportStatus? status,
        int page,
        int limit,
        CancellationToken cancellationToken = default, ContentReportAdminFilter? filter = null);

    Task<int> CountByStatusAsync(ReportStatus status, CancellationToken cancellationToken = default);
}
