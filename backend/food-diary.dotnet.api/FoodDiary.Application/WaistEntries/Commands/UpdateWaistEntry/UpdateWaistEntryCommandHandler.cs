using System;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Contracts.WaistEntries;

namespace FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;

public class UpdateWaistEntryCommandHandler(IWaistEntryRepository waistEntryRepository)
    : ICommandHandler<UpdateWaistEntryCommand, Result<WaistEntryResponse>>
{
    public async Task<Result<WaistEntryResponse>> Handle(
        UpdateWaistEntryCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId is null)
        {
            return Result.Failure<WaistEntryResponse>(Errors.User.NotFound());
        }

        var entry = await waistEntryRepository.GetByIdAsync(
            command.WaistEntryId,
            command.UserId.Value,
            asTracking: true,
            cancellationToken);

        if (entry is null)
        {
            return Result.Failure<WaistEntryResponse>(Errors.WaistEntry.NotFound(command.WaistEntryId.Value));
        }

        var normalizedDate = command.Date.Date;
        var existing = await waistEntryRepository.GetByDateAsync(
            command.UserId.Value,
            normalizedDate,
            cancellationToken);

        if (existing is not null && existing.Id != entry.Id)
        {
            return Result.Failure<WaistEntryResponse>(
                Errors.WaistEntry.AlreadyExists(normalizedDate));
        }

        entry.Update(command.Circumference, normalizedDate);
        await waistEntryRepository.UpdateAsync(entry, cancellationToken);
        return Result.Success(entry.ToResponse());
    }
}
