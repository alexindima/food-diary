using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.ContentReports.Common;

public interface IContentReportWriteRepository {
    Task<ContentReport?> GetByIdAsync(
        ContentReportId id,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<bool> HasUserReportedAsync(
        UserId userId,
        ReportTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default);

    Task<ContentReport> AddAsync(ContentReport report, CancellationToken cancellationToken = default);

    Task UpdateAsync(ContentReport report, CancellationToken cancellationToken = default);
}
