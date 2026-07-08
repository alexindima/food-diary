using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;

public sealed class DeleteWaistEntryCommandHandler(
    IWaistEntryWriteRepository waistEntryRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<DeleteWaistEntryCommand, Result> {
    public async Task<Result> Handle(DeleteWaistEntryCommand command, CancellationToken cancellationToken) {
        Result<WaistEntryId> waistEntryIdResult = RequiredIdParser.Parse(
            command.WaistEntryId,
            nameof(command.WaistEntryId),
            "Waist entry id must not be empty.",
            value => new WaistEntryId(value));
        if (waistEntryIdResult.IsFailure) {
            return RequiredIdParser.ToFailure(waistEntryIdResult);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        WaistEntryId waistEntryId = waistEntryIdResult.Value;
        WaistEntry? entry = await waistEntryRepository.GetByIdAsync(
            waistEntryId,
            userId,
            asTracking: true,
            cancellationToken).ConfigureAwait(false);

        if (entry is null) {
            return Result.Failure(Errors.WaistEntry.NotFound(command.WaistEntryId));
        }

        await waistEntryRepository.DeleteAsync(entry, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
