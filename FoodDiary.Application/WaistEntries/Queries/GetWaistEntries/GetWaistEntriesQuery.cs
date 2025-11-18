using System;
using System.Collections.Generic;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.WaistEntries;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.WaistEntries.Queries.GetWaistEntries;

public record GetWaistEntriesQuery(
    UserId? UserId,
    DateTime? DateFrom,
    DateTime? DateTo,
    int? Limit,
    bool Descending
) : IQuery<Result<IReadOnlyList<WaistEntryResponse>>>;
