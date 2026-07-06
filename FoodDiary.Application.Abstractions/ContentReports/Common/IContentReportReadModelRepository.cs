using FoodDiary.Application.Abstractions.ContentReports.Models;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.ContentReports.Common;

public interface IContentReportReadModelRepository {
    Task<(IReadOnlyList<ContentReportAdminReadModel> Items, int Total)> GetPagedAdminReadModelsAsync(
        ReportStatus? status,
        int page,
        int limit,
        CancellationToken cancellationToken = default);
}