using FoodDiary.Modules.ContentReports.Domain.Entities;
using FoodDiary.Modules.ContentReports.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.ContentReports.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.ContentReports.Application.Abstractions.Common;

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
