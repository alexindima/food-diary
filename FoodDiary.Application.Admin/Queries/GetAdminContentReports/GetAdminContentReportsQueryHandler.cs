using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Admin.Internal.Validation;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Admin.Queries.GetAdminContentReports;

public sealed class GetAdminContentReportsQueryHandler(IAdminContentReadService adminContentReadService)
    : IQueryHandler<GetAdminContentReportsQuery, Result<PagedResponse<AdminContentReportModel>>> {
    private const int MaxPageNumber = 10_000;
    private const int MaxPageSize = 100;

    public async Task<Result<PagedResponse<AdminContentReportModel>>> Handle(
        GetAdminContentReportsQuery query,
        CancellationToken cancellationToken) {
        int pageSize = Math.Clamp(query.Limit, 1, MaxPageSize);
        int pageNumber = Math.Clamp(query.Page, 1, MaxPageNumber);

        ReportStatus? status = EnumFilterParser.ParseOptional<ReportStatus>(query.Status);

        PagedResponse<AdminContentReportModel> reports = await adminContentReadService
            .GetContentReportsAsync(status, pageNumber, pageSize, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success(reports);
    }
}
