using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;

public sealed class DeleteWaistEntryCommandHandler(
    IWaistEntryWriteRepository waistEntryRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<DeleteWaistEntryCommand, Result> {
    public async Task<Result> Handle(DeleteWaistEntryCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        if (command.WaistEntryId == Guid.Empty) {
            return Result.Failure(Errors.Validation.Invalid(nameof(command.WaistEntryId), "Waist entry id must not be empty."));
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var waistEntryId = new WaistEntryId(command.WaistEntryId);
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
