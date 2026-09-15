using FoodDiary.Modules.ContentReports.Contracts.Queries.GetContentReportsForAdministration;
using FoodDiary.Modules.ContentReports.Contracts.Models;
using FoodDiary.Modules.ContentReports.Application.Abstractions.Common;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.ContentReports.Application.Queries.GetContentReportsForAdministration;

public sealed class GetContentReportsForAdministrationQueryHandler(IContentReportReadModelRepository readModelRepository) : IRequestHandler<GetContentReportsForAdministrationQuery, (IReadOnlyList<ContentReportAdminReadModel> Items, int Total)> {
    public Task<(IReadOnlyList<ContentReportAdminReadModel> Items, int Total)> Handle(GetContentReportsForAdministrationQuery request, CancellationToken cancellationToken) =>
        readModelRepository.GetPagedAdminReadModelsAsync(request.Status, request.Page, request.Limit, cancellationToken, request.Filter);
}
