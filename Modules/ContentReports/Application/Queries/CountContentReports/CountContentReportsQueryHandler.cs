using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Mediator;

namespace FoodDiary.Application.ContentReports.Queries.CountContentReports;

public sealed class CountContentReportsQueryHandler(IContentReportReadModelRepository readModelRepository) : IRequestHandler<CountContentReportsQuery, int> {
    public Task<int> Handle(CountContentReportsQuery request, CancellationToken cancellationToken) {
        ReportStatus status = request.Status;
        return readModelRepository.CountByStatusAsync(status, cancellationToken);
    }

}
