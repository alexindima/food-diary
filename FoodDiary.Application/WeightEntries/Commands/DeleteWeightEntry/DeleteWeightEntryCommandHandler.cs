using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;

public class DeleteWeightEntryCommandHandler(IWeightEntryRepository weightEntryRepository)
    : ICommandHandler<DeleteWeightEntryCommand, Result<bool>> {
    public async Task<Result<bool>> Handle(DeleteWeightEntryCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId.Value == Guid.Empty) {
            return Result.Failure<bool>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        var entry = await weightEntryRepository.GetByIdAsync(
            command.WeightEntryId,
            userId,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (entry is null) {
            return Result.Failure<bool>(Errors.WeightEntry.NotFound(command.WeightEntryId.Value));
        }

        await weightEntryRepository.DeleteAsync(entry, cancellationToken);
        return Result.Success(true);
    }
}
