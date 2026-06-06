using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Hydration.Mappings;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Application.Hydration.Validators;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;

public class CreateHydrationEntryCommandHandler(
    IHydrationEntryRepository repository,
    IUserRepository userRepository) : ICommandHandler<CreateHydrationEntryCommand, Result<HydrationEntryModel>> {
    public async Task<Result<HydrationEntryModel>> Handle(
        CreateHydrationEntryCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<HydrationEntryModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<HydrationEntryModel>(accessError);
        }

        Result validation = HydrationValidators.ValidateAmount(command.AmountMl);
        if (validation.IsFailure) {
            return Result.Failure<HydrationEntryModel>(validation.Error);
        }

        DateTime timestampUtc = UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(command.TimestampUtc);
        var entry = HydrationEntry.Create(userId, timestampUtc, command.AmountMl);
        await repository.AddAsync(entry, cancellationToken).ConfigureAwait(false);

        return Result.Success(entry.ToModel());
    }
}
