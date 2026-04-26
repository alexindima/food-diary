using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.WeightEntries.Models;

namespace FoodDiary.Application.WeightEntries.Commands.UpdateWeightEntry;

public record UpdateWeightEntryCommand(
    Guid? UserId,
    Guid WeightEntryId,
    DateTime Date,
    double Weight
) : ICommand<Result<WeightEntryModel>>, IUserRequest;
