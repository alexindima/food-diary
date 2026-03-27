using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;

public class DeleteHydrationEntryCommandHandler(IHydrationEntryRepository repository)
    : ICommandHandler<DeleteHydrationEntryCommand, Result<bool>> {
    public async Task<Result<bool>> Handle(DeleteHydrationEntryCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<bool>(Errors.User.NotFound());
        }

        var userId = new UserId(command.UserId.Value);
        var hydrationEntryId = new HydrationEntryId(command.HydrationEntryId);

        var entry = await repository.GetByIdAsync(hydrationEntryId, asTracking: true, cancellationToken: cancellationToken);
        if (entry is null || entry.UserId != userId) {
            return Result.Failure<bool>(Errors.HydrationEntry.NotFound(command.HydrationEntryId));
        }

        await repository.DeleteAsync(entry, cancellationToken);
        return Result.Success(true);
    }
}
