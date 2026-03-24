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
        var weightEntryId = new WeightEntryId(command.WeightEntryId);
        var entry = await weightEntryRepository.GetByIdAsync(
            weightEntryId,
            userId,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (entry is null) {
            return Result.Failure<bool>(Errors.WeightEntry.NotFound(command.WeightEntryId));
        }

        await weightEntryRepository.DeleteAsync(entry, cancellationToken);
        return Result.Success(true);
    }
}
