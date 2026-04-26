using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;

public class UpdateWaistEntryCommandHandler(
    IWaistEntryRepository waistEntryRepository,
    IUserRepository userRepository)
    : ICommandHandler<UpdateWaistEntryCommand, Result<WaistEntryModel>> {
    public async Task<Result<WaistEntryModel>> Handle(
        UpdateWaistEntryCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<WaistEntryModel>(Errors.Authentication.InvalidToken);
        }

        if (command.WaistEntryId == Guid.Empty) {
            return Result.Failure<WaistEntryModel>(
                Errors.Validation.Invalid(nameof(command.WaistEntryId), "Waist entry id must not be empty."));
        }

        var userId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<WaistEntryModel>(accessError);
        }

        var waistEntryId = new WaistEntryId(command.WaistEntryId);
        var entry = await waistEntryRepository.GetByIdAsync(
            waistEntryId,
            userId,
            asTracking: true,
            cancellationToken);

        if (entry is null) {
            return Result.Failure<WaistEntryModel>(Errors.WaistEntry.NotFound(command.WaistEntryId));
        }

        var normalizedDate = UtcDateNormalizer.NormalizeDateUsingLocalFallback(command.Date);
        var existing = await waistEntryRepository.GetByDateAsync(
            userId,
            normalizedDate,
            cancellationToken);

        if (existing is not null && existing.Id != entry.Id) {
            return Result.Failure<WaistEntryModel>(
                Errors.WaistEntry.AlreadyExists(normalizedDate));
        }

        entry.Update(command.Circumference, normalizedDate);
        await waistEntryRepository.UpdateAsync(entry, cancellationToken);
        return Result.Success(entry.ToModel());
    }
}
