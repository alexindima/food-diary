using FoodDiary.Modules.Admin.Application.Abstractions.Models;

namespace FoodDiary.Modules.Admin.Application.Abstractions.Common;

public interface IAdminBugReportReader {
    Task<AdminBugReportPage> GetPageAsync(AdminBugReportFilter filter, CancellationToken cancellationToken);
}
