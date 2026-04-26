using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Mappings;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.WeightEntries.Commands.CreateWeightEntry;

public class CreateWeightEntryCommandHandler(
    IWeightEntryRepository weightEntryRepository,
    IUserRepository userRepository)
    : ICommandHandler<CreateWeightEntryCommand, Result<WeightEntryModel>> {
    public async Task<Result<WeightEntryModel>> Handle(
        CreateWeightEntryCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<WeightEntryModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<WeightEntryModel>(accessError);
        }

        var normalizedDate = UtcDateNormalizer.NormalizeDateUsingLocalFallback(command.Date);
        var existing = await weightEntryRepository.GetByDateAsync(
            userId,
            normalizedDate,
            cancellationToken);
        if (existing is not null) {
            return Result.Failure<WeightEntryModel>(
                Errors.WeightEntry.AlreadyExists(normalizedDate));
        }

        var entry = WeightEntry.Create(userId, normalizedDate, command.Weight);
        entry = await weightEntryRepository.AddAsync(entry, cancellationToken);

        return Result.Success(entry.ToModel());
    }
}
