using FoodDiary.Modules.ContentReports.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Mediator;
using OwnerDismissContentReportCommand = FoodDiary.Modules.ContentReports.Contracts.Commands.DismissContentReport.DismissContentReportCommand;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Admin.Application.Commands.DismissContentReport;

public sealed class DismissContentReportCommandHandler(ISender administrationService)
    : ICommandHandler<DismissContentReportCommand, Result> {
    public async Task<Result> Handle(DismissContentReportCommand command, CancellationToken cancellationToken) {
        return await administrationService.Send(new OwnerDismissContentReportCommand(ReportId: (ContentReportId)command.ReportId, ReviewerUserId: (UserId)command.ReviewerUserId, AdminNote: command.AdminNote), cancellationToken)
            .ConfigureAwait(false);
    }
}
