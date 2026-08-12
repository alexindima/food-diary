using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Fasting.Models;

namespace FoodDiary.Application.Fasting.Queries.GetFastingTelemetrySummary;

public sealed record GetFastingTelemetrySummaryQuery(int Hours) : IQuery<Result<FastingTelemetrySummaryModel>>;
