using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Admin;

namespace FoodDiary.Application.Admin.Queries.GetAdminAiUsageSummary;

public sealed record GetAdminAiUsageSummaryQuery(DateOnly? From, DateOnly? To)
    : IQuery<Result<AdminAiUsageSummaryResponse>>;
