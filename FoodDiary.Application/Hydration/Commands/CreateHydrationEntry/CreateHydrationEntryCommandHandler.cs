using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Hydration.Mappings;
using FoodDiary.Application.Hydration.Validators;
using FoodDiary.Contracts.Hydration;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;

public class CreateHydrationEntryCommandHandler(
    IHydrationEntryRepository repository) : ICommandHandler<CreateHydrationEntryCommand, Result<HydrationEntryResponse>> {
    public async Task<Result<HydrationEntryResponse>> Handle(
        CreateHydrationEntryCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == UserId.Empty) {
            return Result.Failure<HydrationEntryResponse>(Errors.User.NotFound());
        }

        var validation = HydrationValidators.ValidateAmount(command.AmountMl);
        if (validation.IsFailure) {
            return Result.Failure<HydrationEntryResponse>(validation.Error);
        }

        var entry = HydrationEntry.Create(command.UserId.Value, command.TimestampUtc, command.AmountMl);
        await repository.AddAsync(entry, cancellationToken);

        return Result.Success(entry.ToResponse());
    }
}
