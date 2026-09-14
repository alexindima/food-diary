using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;

namespace FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.GetLatestWeightEntry;

public record GetLatestWeightEntryQuery(Guid? UserId)
    : IQuery<Result<WeightEntryModel?>>, IUserRequest;
