using System;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Contracts.WaistEntries;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.WaistEntries.Commands.CreateWaistEntry;

public class CreateWaistEntryCommandHandler(IWaistEntryRepository waistEntryRepository)
    : ICommandHandler<CreateWaistEntryCommand, Result<WaistEntryResponse>>
{
    public async Task<Result<WaistEntryResponse>> Handle(
        CreateWaistEntryCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId is null)
        {
            return Result.Failure<WaistEntryResponse>(Errors.User.NotFound());
        }

        var normalizedDate = command.Date.Date;
        var existing = await waistEntryRepository.GetByDateAsync(
            command.UserId.Value,
            normalizedDate,
            cancellationToken);
        if (existing is not null)
        {
            return Result.Failure<WaistEntryResponse>(
                Errors.WaistEntry.AlreadyExists(normalizedDate));
        }

        var entry = WaistEntry.Create(command.UserId.Value, normalizedDate, command.Circumference);
        entry = await waistEntryRepository.AddAsync(entry, cancellationToken);
        return Result.Success(entry.ToResponse());
    }
}

