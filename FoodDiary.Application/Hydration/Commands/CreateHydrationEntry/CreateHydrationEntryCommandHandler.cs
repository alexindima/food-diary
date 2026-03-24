using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Hydration.Mappings;
using FoodDiary.Application.Hydration.Validators;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;

public class CreateHydrationEntryCommandHandler(
    IHydrationEntryRepository repository) : ICommandHandler<CreateHydrationEntryCommand, Result<HydrationEntryModel>> {
    public async Task<Result<HydrationEntryModel>> Handle(
        CreateHydrationEntryCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == UserId.Empty) {
            return Result.Failure<HydrationEntryModel>(Errors.User.NotFound());
        }

        var validation = HydrationValidators.ValidateAmount(command.AmountMl);
        if (validation.IsFailure) {
            return Result.Failure<HydrationEntryModel>(validation.Error);
        }

        var entry = HydrationEntry.Create(command.UserId.Value, command.TimestampUtc, command.AmountMl);
        await repository.AddAsync(entry, cancellationToken);

        return Result.Success(entry.ToModel());
    }
}
