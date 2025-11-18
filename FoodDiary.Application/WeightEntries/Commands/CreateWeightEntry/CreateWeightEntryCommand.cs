using System;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.WeightEntries;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.WeightEntries.Commands.CreateWeightEntry;

public record CreateWeightEntryCommand(
    UserId? UserId,
    DateTime Date,
    double Weight
) : ICommand<Result<WeightEntryResponse>>;
