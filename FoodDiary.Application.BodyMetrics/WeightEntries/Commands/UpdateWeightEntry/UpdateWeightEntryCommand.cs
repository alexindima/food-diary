using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.WeightEntries.Models;

namespace FoodDiary.Application.BodyMetrics.WeightEntries.Commands.UpdateWeightEntry;

public record UpdateWeightEntryCommand(
    Guid? UserId,
    Guid WeightEntryId,
    DateTime Date,
    double WeightKg
) : ICommand<Result<WeightEntryModel>>, IUserRequest;
