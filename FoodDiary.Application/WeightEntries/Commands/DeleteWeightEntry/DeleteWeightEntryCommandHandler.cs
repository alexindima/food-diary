using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;

public sealed class DeleteWeightEntryCommandHandler(
    IWeightEntryWriteRepository weightEntryRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<DeleteWeightEntryCommand, Result> {
    public async Task<Result> Handle(DeleteWeightEntryCommand command, CancellationToken cancellationToken) {
        if (command.WeightEntryId == Guid.Empty) {
            return Result.Failure(Errors.Validation.Invalid(nameof(command.WeightEntryId), "Weight entry id must not be empty."));
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        var weightEntryId = new WeightEntryId(command.WeightEntryId);
        WeightEntry? entry = await weightEntryRepository.GetByIdAsync(
            weightEntryId,
            userId,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (entry is null) {
            return Result.Failure(Errors.WeightEntry.NotFound(command.WeightEntryId));
        }

        await weightEntryRepository.DeleteAsync(entry, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
