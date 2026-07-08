using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Admin.Queries.GetAdminContentReports;

public sealed class GetAdminContentReportsQueryHandler(IAdminContentReadService adminContentReadService)
    : IQueryHandler<GetAdminContentReportsQuery, Result<PagedResponse<AdminContentReportModel>>> {
    public async Task<Result<PagedResponse<AdminContentReportModel>>> Handle(
        GetAdminContentReportsQuery query,
        CancellationToken cancellationToken) {
        int pageNumber = Math.Max(query.Page, 1);
        int pageSize = Math.Max(query.Limit, 1);

        ReportStatus? status = EnumFilterParser.ParseOptional<ReportStatus>(query.Status);

        PagedResponse<AdminContentReportModel> reports = await adminContentReadService
            .GetContentReportsAsync(status, pageNumber, pageSize, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success(reports);
    }
}
