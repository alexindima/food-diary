using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.BodyMetrics.Application.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.BodyMetrics.Domain.ValueObjects.Ids;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;

namespace FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Commands.DeleteWeightEntry;

public sealed class DeleteWeightEntryCommandHandler(
    IWeightEntryWriteRepository weightEntryRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<DeleteWeightEntryCommand, Result> {
    public async Task<Result> Handle(DeleteWeightEntryCommand command, CancellationToken cancellationToken) {
        Result<WeightEntryId> weightEntryIdResult = RequiredIdParser.Parse(
            command.WeightEntryId,
            nameof(command.WeightEntryId),
            "WeightKg entry id must not be empty.",
            value => new WeightEntryId(value));
        if (weightEntryIdResult.IsFailure) {
            return RequiredIdParser.ToFailure(weightEntryIdResult);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        WeightEntryId weightEntryId = weightEntryIdResult.Value;
        WeightEntry? entry = await weightEntryRepository.GetByIdAsync(
            weightEntryId,
            userId,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (entry is null) {
            return Result.Failure(WeightEntryErrors.NotAccessible(command.WeightEntryId));
        }

        await weightEntryRepository.DeleteAsync(entry, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
