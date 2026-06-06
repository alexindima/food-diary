using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.WaistEntries.Commands.CreateWaistEntry;

public class CreateWaistEntryCommandHandler(
    IWaistEntryRepository waistEntryRepository,
    IUserRepository userRepository)
    : ICommandHandler<CreateWaistEntryCommand, Result<WaistEntryModel>> {
    public async Task<Result<WaistEntryModel>> Handle(
        CreateWaistEntryCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<WaistEntryModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<WaistEntryModel>(accessError);
        }

        DateTime normalizedDate = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(command.Date);
        WaistEntry? existing = await waistEntryRepository.GetByDateAsync(
            userId,
            normalizedDate,
            cancellationToken).ConfigureAwait(false);
        if (existing is not null) {
            return Result.Failure<WaistEntryModel>(
                Errors.WaistEntry.AlreadyExists(normalizedDate));
        }

        var entry = WaistEntry.Create(userId, normalizedDate, command.Circumference);
        entry = await waistEntryRepository.AddAsync(entry, cancellationToken).ConfigureAwait(false);
        return Result.Success(entry.ToModel());
    }
}
