using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;

namespace FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Commands.UpdateWeightEntry;

public record UpdateWeightEntryCommand(
    Guid? UserId,
    Guid WeightEntryId,
    DateTime Date,
    double WeightKg
) : ICommand<Result<WeightEntryModel>>, IUserRequest;
