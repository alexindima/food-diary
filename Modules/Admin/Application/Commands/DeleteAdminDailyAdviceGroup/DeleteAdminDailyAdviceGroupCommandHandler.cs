using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Mediator;
using FoodDiary.Modules.DailyAdvices.Contracts.Commands.DeleteDailyAdviceGroup;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.DeleteAdminDailyAdviceGroup;

public sealed class DeleteAdminDailyAdviceGroupCommandHandler(ISender sender) : ICommandHandler<DeleteAdminDailyAdviceGroupCommand, Result> {
    public async Task<Result> Handle(DeleteAdminDailyAdviceGroupCommand command, CancellationToken cancellationToken) {
        return await sender.Send(new DeleteDailyAdviceGroupCommand(command.Id), cancellationToken).ConfigureAwait(false);
    }
}
