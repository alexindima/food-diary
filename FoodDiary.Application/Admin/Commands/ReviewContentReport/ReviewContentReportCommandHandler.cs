using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.ContentReports.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Commands.ReviewContentReport;

public sealed class ReviewContentReportCommandHandler(IContentReportAdministrationService administrationService)
    : ICommandHandler<ReviewContentReportCommand, Result> {
    public async Task<Result> Handle(ReviewContentReportCommand command, CancellationToken cancellationToken) {
        return await administrationService
            .MarkReviewedAsync((ContentReportId)command.ReportId, command.AdminNote, cancellationToken)
            .ConfigureAwait(false);
    }
}
