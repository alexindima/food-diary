using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.WeightEntries.Models;

namespace FoodDiary.Application.WeightEntries.Commands.CreateWeightEntry;

public record CreateWeightEntryCommand(
    Guid? UserId,
    DateTime Date,
    double Weight
) : ICommand<Result<WeightEntryModel>>, IUserRequest;
