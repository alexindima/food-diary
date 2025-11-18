using System;
using System.Collections.Generic;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.WaistEntries;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;

public record GetWaistSummariesQuery(
    UserId? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays
) : IQuery<Result<IReadOnlyList<WaistEntrySummaryResponse>>>;
