using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.WeightEntries.Models;

namespace FoodDiary.Application.BodyMetrics.WeightEntries.Commands.CreateWeightEntry;

public record CreateWeightEntryCommand(
    Guid? UserId,
    DateTime Date,
    double Weight
) : ICommand<Result<WeightEntryModel>>, IUserRequest;
