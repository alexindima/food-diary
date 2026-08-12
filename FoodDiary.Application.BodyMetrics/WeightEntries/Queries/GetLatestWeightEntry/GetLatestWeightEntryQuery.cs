using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.WeightEntries.Models;

namespace FoodDiary.Application.WeightEntries.Queries.GetLatestWeightEntry;

public record GetLatestWeightEntryQuery(Guid? UserId)
    : IQuery<Result<WeightEntryModel?>>, IUserRequest;
