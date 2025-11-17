using System;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.WaistEntries;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.WaistEntries.Commands.CreateWaistEntry;

public record CreateWaistEntryCommand(
    UserId? UserId,
    DateTime Date,
    double Circumference
) : ICommand<Result<WaistEntryResponse>>;
