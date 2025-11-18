using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;

namespace FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;

public class DeleteWaistEntryCommandHandler(IWaistEntryRepository waistEntryRepository)
    : ICommandHandler<DeleteWaistEntryCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteWaistEntryCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId is null)
        {
            return Result.Failure<bool>(Errors.User.NotFound());
        }

        var entry = await waistEntryRepository.GetByIdAsync(
            command.WaistEntryId,
            command.UserId.Value,
            asTracking: true,
            cancellationToken);

        if (entry is null)
        {
            return Result.Failure<bool>(Errors.WaistEntry.NotFound(command.WaistEntryId.Value));
        }

        await waistEntryRepository.DeleteAsync(entry, cancellationToken);
        return Result.Success(true);
    }
}
