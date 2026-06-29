using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Queries.GetFastingTelemetrySummary;

public sealed record GetFastingTelemetrySummaryQuery(int Hours) : IQuery<Result<FastingTelemetrySummaryModel>>;
