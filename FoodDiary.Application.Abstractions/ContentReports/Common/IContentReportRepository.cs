using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ContentReports.Common;

public interface IContentReportRepository {
    Task<ContentReport> AddAsync(ContentReport report, CancellationToken cancellationToken = default);

    Task<ContentReport?> GetByIdAsync(
        ContentReportId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(ContentReport report, CancellationToken cancellationToken = default);

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

    Task<int> CountByStatusAsync(ReportStatus status, CancellationToken cancellationToken = default);
}
