using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Mappings;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.WeightEntries.Commands.CreateWeightEntry;

public class CreateWeightEntryCommandHandler(
    IWeightEntryRepository weightEntryRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<CreateWeightEntryCommand, Result<WeightEntryModel>> {
    public async Task<Result<WeightEntryModel>> Handle(
        CreateWeightEntryCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<WeightEntryModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<WeightEntryModel>(accessError);
        }

        DateTime normalizedDate = UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(command.Date);
        WeightEntry? existing = await weightEntryRepository.GetByDateAsync(
            userId,
            normalizedDate,
            cancellationToken).ConfigureAwait(false);
        if (existing is not null) {
            return Result.Failure<WeightEntryModel>(
                Errors.WeightEntry.AlreadyExists(normalizedDate));
        }

        var entry = WeightEntry.Create(userId, normalizedDate, command.Weight);
        entry = await weightEntryRepository.AddAsync(entry, cancellationToken).ConfigureAwait(false);

        return Result.Success(entry.ToModel());
    }
}
