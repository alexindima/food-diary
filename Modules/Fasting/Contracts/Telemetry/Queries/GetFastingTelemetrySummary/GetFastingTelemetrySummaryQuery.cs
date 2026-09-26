using FoodDiary.Modules.Fasting.Contracts.Telemetry.Models;
using FoodDiary.Results;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Fasting.Contracts.Telemetry.Queries.GetFastingTelemetrySummary;

public sealed record GetFastingTelemetrySummaryQuery(int Hours) : IQuery<Result<FastingTelemetrySummaryModel>>;
