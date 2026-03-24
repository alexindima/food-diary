using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;

public class DeleteWaistEntryCommandHandler(IWaistEntryRepository waistEntryRepository)
    : ICommandHandler<DeleteWaistEntryCommand, Result<bool>> {
    public async Task<Result<bool>> Handle(DeleteWaistEntryCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId.Value == Guid.Empty) {
            return Result.Failure<bool>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        var waistEntryId = new WaistEntryId(command.WaistEntryId);
        var entry = await waistEntryRepository.GetByIdAsync(
            waistEntryId,
            userId,
            asTracking: true,
            cancellationToken);

        if (entry is null) {
            return Result.Failure<bool>(Errors.WaistEntry.NotFound(command.WaistEntryId));
        }

        await waistEntryRepository.DeleteAsync(entry, cancellationToken);
        return Result.Success(true);
    }
}
