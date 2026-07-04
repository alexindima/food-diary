using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Admin.Queries.GetAdminContentReports;

public sealed class GetAdminContentReportsQueryHandler(IContentReportReadRepository reportRepository)
    : IQueryHandler<GetAdminContentReportsQuery, Result<PagedResponse<AdminContentReportModel>>> {
    public async Task<Result<PagedResponse<AdminContentReportModel>>> Handle(
        GetAdminContentReportsQuery query,
        CancellationToken cancellationToken) {
        int pageNumber = Math.Max(query.Page, 1);
        int pageSize = Math.Max(query.Limit, 1);

        ReportStatus? status = EnumFilterParser.ParseOptional<ReportStatus>(query.Status);

        (IReadOnlyList<ContentReport> items, int total) = await reportRepository.GetPagedAsync(status, pageNumber, pageSize, cancellationToken).ConfigureAwait(false);

        var models = items.Select(static report => report.ToAdminModel()).ToList();

        int totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Result.Success(new PagedResponse<AdminContentReportModel>(models, pageNumber, pageSize, totalPages, total));
    }
}
