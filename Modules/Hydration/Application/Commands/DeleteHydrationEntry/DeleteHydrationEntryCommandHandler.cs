using FoodDiary.Modules.Hydration.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Hydration.Application.Internal;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Hydration.Application.Abstractions.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Hydration.Application.Commands.DeleteHydrationEntry;

public sealed class DeleteHydrationEntryCommandHandler(
    IHydrationEntryWriteRepository repository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<DeleteHydrationEntryCommand, Result> {
    public async Task<Result> Handle(DeleteHydrationEntryCommand command, CancellationToken cancellationToken) {
        Result<HydrationEntryId> hydrationEntryIdResult = RequiredIdParser.Parse(
            command.HydrationEntryId,
            nameof(command.HydrationEntryId),
            "Hydration entry id must not be empty.",
            value => new HydrationEntryId(value));
        if (hydrationEntryIdResult.IsFailure) {
            return RequiredIdParser.ToFailure(hydrationEntryIdResult);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        HydrationEntryId hydrationEntryId = hydrationEntryIdResult.Value;

        HydrationEntry? entry = await repository.GetByIdForUpdateAsync(hydrationEntryId, cancellationToken).ConfigureAwait(false);
        if (entry is null || entry.UserId != userId) {
            return Result.Failure(HydrationEntryErrors.NotAccessible(command.HydrationEntryId));
        }

        await repository.DeleteAsync(entry, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
