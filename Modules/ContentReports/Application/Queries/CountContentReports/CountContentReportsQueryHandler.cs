using FoodDiary.Modules.ContentReports.Contracts.Queries.CountContentReports;
using FoodDiary.Modules.ContentReports.Application.Abstractions.Common;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.ContentReports.Application.Queries.CountContentReports;

public sealed class CountContentReportsQueryHandler(IContentReportReadModelRepository readModelRepository) : IRequestHandler<CountContentReportsQuery, int> {
    public Task<int> Handle(CountContentReportsQuery request, CancellationToken cancellationToken) =>
        readModelRepository.CountByStatusAsync(request.Status, cancellationToken);
}
