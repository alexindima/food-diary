using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Application.ContentReports.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Mediator;

namespace FoodDiary.Application.ContentReports.Queries.GetContentReportsForAdministration;

public sealed class GetContentReportsForAdministrationQueryHandler(IContentReportReadModelRepository readModelRepository) : IRequestHandler<GetContentReportsForAdministrationQuery, (IReadOnlyList<ContentReportAdminReadModel> Items, int Total)> {
    public Task<(IReadOnlyList<ContentReportAdminReadModel> Items, int Total)> Handle(GetContentReportsForAdministrationQuery request, CancellationToken cancellationToken) {
        ReportStatus? status = request.Status;
        int page = request.Page;
        int limit = request.Limit;
        ContentReportAdminFilter? filter = request.Filter;
        return readModelRepository.GetPagedAdminReadModelsAsync(status, page, limit, cancellationToken, filter);
    }

}
