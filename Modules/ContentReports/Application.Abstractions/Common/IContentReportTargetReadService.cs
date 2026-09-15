using FoodDiary.Modules.ContentReports.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.ContentReports.Application.Abstractions.Common;

public interface IContentReportTargetReadService {
    Task<bool> IsReportableAsync(
        UserId reporterUserId,
        ReportTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default);
}
