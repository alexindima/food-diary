using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Mediator;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Modules.DailyAdvices.Contracts.Commands.UpdateDailyAdviceGroup;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Commands.UpdateAdminDailyAdviceGroup;

public sealed class UpdateAdminDailyAdviceGroupCommandHandler(ISender sender) : ICommandHandler<UpdateAdminDailyAdviceGroupCommand, Result<AdminDailyAdviceGroupModel>> {
    public async Task<Result<AdminDailyAdviceGroupModel>> Handle(UpdateAdminDailyAdviceGroupCommand command, CancellationToken cancellationToken) {
        Result<DailyAdviceGroupModel> result = await sender.Send(new UpdateDailyAdviceGroupCommand(command.Id, command.Ru, command.En, command.Weight, command.Tag), cancellationToken).ConfigureAwait(false);
        return result.IsFailure ? Result.Failure<AdminDailyAdviceGroupModel>(result.Error)
            : Result.Success(new AdminDailyAdviceGroupModel(result.Value.Id, result.Value.Ru, result.Value.En, result.Value.Weight, result.Value.Tag));
    }
}
