using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Admin.Internal.Validation;
using FoodDiary.Application.Admin.Mappings;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.ContentReports.Common;
using FoodDiary.Application.ContentReports.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminContentReports;

public sealed class GetAdminContentReportsQueryHandler(IContentReportAdministrationReadService contentReportReadService)
    : IQueryHandler<GetAdminContentReportsQuery, Result<PagedResponse<AdminContentReportModel>>> {
    public async Task<Result<PagedResponse<AdminContentReportModel>>> Handle(GetAdminContentReportsQuery query, CancellationToken cancellationToken) {
        int pageSize = PaginationPolicy.NormalizePageSize(query.Limit, defaultPageSize: 1);
        int pageNumber = PaginationPolicy.NormalizePage(query.Page);
        ReportStatus? status = EnumFilterParser.ParseOptional<ReportStatus>(query.Status);

        (IReadOnlyList<ContentReportAdminReadModel> items, int total) = await contentReportReadService
            .GetReportsAsync(status, pageNumber, pageSize, cancellationToken, new ContentReportAdminFilter(query.FromUtc, query.ToUtc, query.TargetType, query.ReporterId, query.TargetId))
            .ConfigureAwait(false);

        IReadOnlyList<AdminContentReportModel> models = [
            .. items.Select(static report => report.ToAdminModel()),
        ];

        int totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Result.Success(new PagedResponse<AdminContentReportModel>(models, pageNumber, pageSize, totalPages, total));
    }
}
