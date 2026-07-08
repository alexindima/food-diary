using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.WeightEntries.Models;

namespace FoodDiary.Application.WeightEntries.Queries.GetLatestWeightEntry;

public record GetLatestWeightEntryQuery(Guid? UserId)
    : IQuery<Result<WeightEntryModel?>>, IUserRequest;
