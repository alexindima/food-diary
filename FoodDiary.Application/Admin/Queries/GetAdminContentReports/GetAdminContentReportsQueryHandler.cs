using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.ContentReports.Common;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Admin.Queries.GetAdminContentReports;

public sealed class GetAdminContentReportsQueryHandler(IContentReportRepository reportRepository)
    : IQueryHandler<GetAdminContentReportsQuery, Result<PagedResponse<AdminContentReportModel>>> {
    public async Task<Result<PagedResponse<AdminContentReportModel>>> Handle(
        GetAdminContentReportsQuery query,
        CancellationToken cancellationToken) {
        var pageNumber = Math.Max(query.Page, 1);
        var pageSize = Math.Max(query.Limit, 1);

        ReportStatus? status = query.Status is not null && Enum.TryParse<ReportStatus>(query.Status, out var parsed)
            ? parsed
            : null;

        var (items, total) = await reportRepository.GetPagedAsync(status, pageNumber, pageSize, cancellationToken);

        var models = items.Select(r => new AdminContentReportModel(
            r.Id.Value,
            r.UserId.Value,
            r.TargetType.ToString(),
            r.TargetId,
            r.Reason,
            r.Status.ToString(),
            r.AdminNote,
            r.CreatedOnUtc,
            r.ReviewedAtUtc)).ToList();

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Result.Success(new PagedResponse<AdminContentReportModel>(models, pageNumber, pageSize, totalPages, total));
    }
}
