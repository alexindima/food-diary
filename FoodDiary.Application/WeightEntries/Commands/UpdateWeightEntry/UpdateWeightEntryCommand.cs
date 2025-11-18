using System;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.WeightEntries;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.WeightEntries.Commands.UpdateWeightEntry;

public record UpdateWeightEntryCommand(
    UserId? UserId,
    WeightEntryId WeightEntryId,
    DateTime Date,
    double Weight
) : ICommand<Result<WeightEntryResponse>>;
