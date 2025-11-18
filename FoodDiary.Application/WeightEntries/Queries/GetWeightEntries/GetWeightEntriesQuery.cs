using System;
using System.Collections.Generic;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.WeightEntries;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.WeightEntries.Queries.GetWeightEntries;

public record GetWeightEntriesQuery(
    UserId? UserId,
    DateTime? DateFrom,
    DateTime? DateTo,
    int? Limit,
    bool Descending
) : IQuery<Result<IReadOnlyList<WeightEntryResponse>>>;
