using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Fasting.Application.Queries.GetFastingTelemetrySummary;

public sealed record GetFastingTelemetrySummaryQuery(int Hours) : IQuery<Result<FastingTelemetrySummaryModel>>;
