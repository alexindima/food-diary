using FoodDiary.Application.Abstractions.ContentReports.Models;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.ContentReports.Common;

public interface IContentReportAdministrationReadService {
    Task<(IReadOnlyList<ContentReportAdminReadModel> Items, int Total)> GetReportsAsync(
        ReportStatus? status,
        int page,
        int limit,
        CancellationToken cancellationToken);

    Task<int> CountAsync(ReportStatus status, CancellationToken cancellationToken);
}
