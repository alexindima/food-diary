using FoodDiary.Application.Abstractions.Admin.Models;

namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IAdminBugReportReader {
    Task<AdminBugReportPage> GetPageAsync(AdminBugReportFilter filter, CancellationToken cancellationToken);
}
