using System;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.WaistEntries;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;

public record UpdateWaistEntryCommand(
    UserId? UserId,
    WaistEntryId WaistEntryId,
    DateTime Date,
    double Circumference
) : ICommand<Result<WaistEntryResponse>>;
