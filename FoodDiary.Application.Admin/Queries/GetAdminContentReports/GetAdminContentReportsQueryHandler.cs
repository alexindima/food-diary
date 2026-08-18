using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Admin.Internal.Validation;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Admin.Queries.GetAdminContentReports;

public sealed class GetAdminContentReportsQueryHandler(IAdminContentReadService adminContentReadService)
    : IQueryHandler<GetAdminContentReportsQuery, Result<PagedResponse<AdminContentReportModel>>> {
    public async Task<Result<PagedResponse<AdminContentReportModel>>> Handle(
        GetAdminContentReportsQuery query,
        CancellationToken cancellationToken) {
        int pageSize = PaginationPolicy.NormalizePageSize(query.Limit, defaultPageSize: 1);
        int pageNumber = PaginationPolicy.NormalizePage(query.Page);

        ReportStatus? status = EnumFilterParser.ParseOptional<ReportStatus>(query.Status);

        PagedResponse<AdminContentReportModel> reports = await adminContentReadService
            .GetContentReportsAsync(status, pageNumber, pageSize, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success(reports);
    }
}
