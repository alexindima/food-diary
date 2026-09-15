using FoodDiary.Modules.ContentReports.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Mediator;
using OwnerReviewContentReportCommand = FoodDiary.Modules.ContentReports.Contracts.Commands.ReviewContentReport.ReviewContentReportCommand;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Admin.Application.Commands.ReviewContentReport;

public sealed class ReviewContentReportCommandHandler(ISender administrationService)
    : ICommandHandler<ReviewContentReportCommand, Result> {
    public async Task<Result> Handle(ReviewContentReportCommand command, CancellationToken cancellationToken) {
        return await administrationService.Send(new OwnerReviewContentReportCommand(ReportId: (ContentReportId)command.ReportId, ReviewerUserId: (UserId)command.ReviewerUserId, AdminNote: command.AdminNote), cancellationToken)
            .ConfigureAwait(false);
    }
}
