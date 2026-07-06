using FoodDiary.Application.Abstractions.ContentReports.Models;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.ContentReports.Common;

public interface IContentReportReadRepository {
    Task<ContentReport?> GetByIdAsync(
        ContentReportId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<bool> HasUserReportedAsync(
        UserId userId,
        ReportTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ContentReport> Items, int Total)> GetPagedAsync(
        ReportStatus? status,
        int page,
        int limit,
        CancellationToken cancellationToken = default);

    async Task<(IReadOnlyList<ContentReportAdminReadModel> Items, int Total)> GetPagedAdminReadModelsAsync(
        ReportStatus? status,
        int page,
        int limit,
        CancellationToken cancellationToken = default) {
        (IReadOnlyList<ContentReport> Items, int Total) = await GetPagedAsync(
            status,
            page,
            limit,
            cancellationToken).ConfigureAwait(false);

        return ([.. Items.Select(ToAdminReadModel)], Total);
    }

    Task<int> CountByStatusAsync(ReportStatus status, CancellationToken cancellationToken = default);

    private static ContentReportAdminReadModel ToAdminReadModel(ContentReport report) =>
        new(
            report.Id.Value,
            report.UserId.Value,
            report.TargetType.ToString(),
            report.TargetId,
            report.Reason,
            report.Status.ToString(),
            report.AdminNote,
            report.CreatedOnUtc,
            report.ReviewedAtUtc);
}
