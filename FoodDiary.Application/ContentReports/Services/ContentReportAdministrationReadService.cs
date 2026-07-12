using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Application.Abstractions.ContentReports.Models;
using FoodDiary.Application.ContentReports.Common;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.ContentReports.Services;

public sealed class ContentReportAdministrationReadService(
    IContentReportReadModelRepository readModelRepository,
    IContentReportReadRepository readRepository) : IContentReportAdministrationReadService {
    public Task<(IReadOnlyList<ContentReportAdminReadModel> Items, int Total)> GetReportsAsync(
        ReportStatus? status,
        int page,
        int limit,
        CancellationToken cancellationToken) =>
        readModelRepository.GetPagedAdminReadModelsAsync(status, page, limit, cancellationToken);

    public Task<int> CountAsync(ReportStatus status, CancellationToken cancellationToken) =>
        readRepository.CountByStatusAsync(status, cancellationToken);
}
