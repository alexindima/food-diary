using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.WeightEntries.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;

public class DeleteWeightEntryCommandHandler(IWeightEntryRepository weightEntryRepository)
    : ICommandHandler<DeleteWeightEntryCommand, Result> {
    public async Task<Result> Handle(DeleteWeightEntryCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var weightEntryId = new WeightEntryId(command.WeightEntryId);
        var entry = await weightEntryRepository.GetByIdAsync(
            weightEntryId,
            userId,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (entry is null) {
            return Result.Failure(Errors.WeightEntry.NotFound(command.WeightEntryId));
        }

        await weightEntryRepository.DeleteAsync(entry, cancellationToken);
        return Result.Success();
    }
}
