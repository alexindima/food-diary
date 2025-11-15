using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;

namespace FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;

public class DeleteWeightEntryCommandHandler(IWeightEntryRepository weightEntryRepository)
    : ICommandHandler<DeleteWeightEntryCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteWeightEntryCommand command, CancellationToken cancellationToken)
    {
        if (command.UserId is null)
        {
            return Result.Failure<bool>(Errors.User.NotFound());
        }

        var entry = await weightEntryRepository.GetByIdAsync(
            command.WeightEntryId,
            command.UserId.Value,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (entry is null)
        {
            return Result.Failure<bool>(Errors.WeightEntry.NotFound(command.WeightEntryId.Value));
        }

        await weightEntryRepository.DeleteAsync(entry, cancellationToken);
        return Result.Success(true);
    }
}
