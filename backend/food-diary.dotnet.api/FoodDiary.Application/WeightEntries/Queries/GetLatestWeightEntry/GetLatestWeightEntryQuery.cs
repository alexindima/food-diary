using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.WeightEntries;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.WeightEntries.Queries.GetLatestWeightEntry;

public record GetLatestWeightEntryQuery(UserId? UserId)
    : IQuery<Result<WeightEntryResponse?>>;
