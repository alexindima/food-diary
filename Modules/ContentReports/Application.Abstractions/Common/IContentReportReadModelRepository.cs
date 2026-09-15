using FoodDiary.Modules.ContentReports.Domain.Contracts.Enums;
using FoodDiary.Modules.ContentReports.Contracts.Models;

namespace FoodDiary.Modules.ContentReports.Application.Abstractions.Common;

public interface IContentReportReadModelRepository {
    Task<(IReadOnlyList<ContentReportAdminReadModel> Items, int Total)> GetPagedAdminReadModelsAsync(
        ReportStatus? status,
        int page,
        int limit,
        CancellationToken cancellationToken = default, ContentReportAdminFilter? filter = null);

    Task<int> CountByStatusAsync(ReportStatus status, CancellationToken cancellationToken = default);
}
