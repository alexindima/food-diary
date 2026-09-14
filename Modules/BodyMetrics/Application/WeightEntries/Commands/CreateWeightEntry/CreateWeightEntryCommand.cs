using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;

namespace FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Commands.CreateWeightEntry;

public record CreateWeightEntryCommand(
    Guid? UserId,
    DateTime Date,
    double WeightKg
) : ICommand<Result<WeightEntryModel>>, IUserRequest;
