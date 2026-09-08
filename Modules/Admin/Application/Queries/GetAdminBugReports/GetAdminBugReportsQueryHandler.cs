using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminBugReports;

public sealed class GetAdminBugReportsQueryHandler(IAdminBugReportReader reader) : IQueryHandler<GetAdminBugReportsQuery, Result<AdminBugReportPage>> {
    public async Task<Result<AdminBugReportPage>> Handle(GetAdminBugReportsQuery query, CancellationToken cancellationToken) =>
        Result.Success(await reader.GetPageAsync(query.Filter, cancellationToken).ConfigureAwait(false));
}
