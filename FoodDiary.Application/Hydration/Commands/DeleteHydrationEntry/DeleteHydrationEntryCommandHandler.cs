using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;

public sealed class DeleteHydrationEntryCommandHandler(
    IHydrationEntryWriteRepository repository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<DeleteHydrationEntryCommand, Result> {
    public async Task<Result> Handle(DeleteHydrationEntryCommand command, CancellationToken cancellationToken) {
        if (command.HydrationEntryId == Guid.Empty) {
            return Result.Failure(Errors.Validation.Invalid(nameof(command.HydrationEntryId), "Hydration entry id must not be empty."));
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        var hydrationEntryId = new HydrationEntryId(command.HydrationEntryId);

        HydrationEntry? entry = await repository.GetByIdAsync(hydrationEntryId, asTracking: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (entry is null || entry.UserId != userId) {
            return Result.Failure(Errors.HydrationEntry.NotFound(command.HydrationEntryId));
        }

        await repository.DeleteAsync(entry, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
