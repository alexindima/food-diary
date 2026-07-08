using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminAiUsageSummary;

public sealed record GetAdminAiUsageSummaryQuery(DateOnly? From, DateOnly? To)
    : IQuery<Result<AdminAiUsageSummaryModel>>;
