using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.ContentReports.Common;

public interface IContentReportAdministrationService {
    Task<Result> MarkReviewedAsync(
        ContentReportId reportId,
        UserId reviewerUserId,
        string? adminNote,
        CancellationToken cancellationToken);

    Task<Result> MarkDismissedAsync(
        ContentReportId reportId,
        UserId reviewerUserId,
        string? adminNote,
        CancellationToken cancellationToken);
}
