using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeightEntries.Commands.CreateWeightEntry;

public record CreateWeightEntryCommand(
    UserId? UserId,
    DateTime Date,
    double Weight
) : ICommand<Result<WeightEntryModel>>;
