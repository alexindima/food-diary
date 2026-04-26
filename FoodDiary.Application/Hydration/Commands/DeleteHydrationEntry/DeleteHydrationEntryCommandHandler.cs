using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;

public class DeleteHydrationEntryCommandHandler(
    IHydrationEntryRepository repository,
    IUserRepository userRepository)
    : ICommandHandler<DeleteHydrationEntryCommand, Result> {
    public async Task<Result> Handle(DeleteHydrationEntryCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        if (command.HydrationEntryId == Guid.Empty) {
            return Result.Failure(Errors.Validation.Invalid(nameof(command.HydrationEntryId), "Hydration entry id must not be empty."));
        }

        var userId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var hydrationEntryId = new HydrationEntryId(command.HydrationEntryId);

        var entry = await repository.GetByIdAsync(hydrationEntryId, asTracking: true, cancellationToken: cancellationToken);
        if (entry is null || entry.UserId != userId) {
            return Result.Failure(Errors.HydrationEntry.NotFound(command.HydrationEntryId));
        }

        await repository.DeleteAsync(entry, cancellationToken);
        return Result.Success();
    }
}
