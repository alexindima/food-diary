using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Hydration.Mappings;
using FoodDiary.Application.Hydration.Validators;
using FoodDiary.Contracts.Hydration;

namespace FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;

public class UpdateHydrationEntryCommandHandler(
    IHydrationEntryRepository repository) : ICommandHandler<UpdateHydrationEntryCommand, Result<HydrationEntryResponse>>
{
    public async Task<Result<HydrationEntryResponse>> Handle(
        UpdateHydrationEntryCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId is null)
        {
            return Result.Failure<HydrationEntryResponse>(Errors.User.NotFound());
        }

        var entry = await repository.GetByIdAsync(
            command.HydrationEntryId,
            asTracking: true,
            cancellationToken: cancellationToken);
        if (entry is null || entry.UserId != command.UserId.Value)
        {
            return Result.Failure<HydrationEntryResponse>(Errors.HydrationEntry.NotFound(command.HydrationEntryId.Value));
        }

        if (command.AmountMl.HasValue)
        {
            var validation = HydrationValidators.ValidateAmount(command.AmountMl.Value);
            if (validation.IsFailure)
            {
                return Result.Failure<HydrationEntryResponse>(validation.Error);
            }
        }

        entry.Update(command.AmountMl, command.TimestampUtc);
        await repository.UpdateAsync(entry, cancellationToken);

        return Result.Success(entry.ToResponse());
    }
}
