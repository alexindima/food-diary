using FoodDiary.Modules.ContentReports.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.ContentReports.Application.Abstractions.Common;

public interface IContentReportTargetReadService {
    Task<bool> IsReportableAsync(
        UserId reporterUserId,
        ReportTarget target,
        CancellationToken cancellationToken = default);
}
