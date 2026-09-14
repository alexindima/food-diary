using FoodDiary.Mediator;
using FoodDiary.Application.ContentReports.Queries.GetContentReportsForAdministration;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Modules.Admin.Application.Internal.Validation;
using FoodDiary.Modules.Admin.Application.Mappings;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.ContentReports.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminContentReports;

public sealed class GetAdminContentReportsQueryHandler(ISender contentReportReadService)
    : IQueryHandler<GetAdminContentReportsQuery, Result<PagedResponse<AdminContentReportModel>>> {
    public async Task<Result<PagedResponse<AdminContentReportModel>>> Handle(GetAdminContentReportsQuery query, CancellationToken cancellationToken) {
        int pageSize = PaginationPolicy.NormalizePageSize(query.Limit, defaultPageSize: 1);
        int pageNumber = PaginationPolicy.NormalizePage(query.Page);
        ReportStatus? status = EnumFilterParser.ParseOptional<ReportStatus>(query.Status);

        (IReadOnlyList<ContentReportAdminReadModel> items, int total) = await contentReportReadService.Send(new GetContentReportsForAdministrationQuery(Status: status, Page: pageNumber, Limit: pageSize, Filter: new ContentReportAdminFilter(query.FromUtc, query.ToUtc, query.TargetType, query.ReporterId, query.TargetId)), cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<AdminContentReportModel> models = [
            .. items.Select(static report => report.ToAdminModel()),
        ];

        int totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Result.Success(new PagedResponse<AdminContentReportModel>(models, pageNumber, pageSize, totalPages, total));
    }
}
