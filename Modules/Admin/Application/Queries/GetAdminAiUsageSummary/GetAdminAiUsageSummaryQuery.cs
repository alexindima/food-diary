using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Admin.Application.Models;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminAiUsageSummary;

public sealed record GetAdminAiUsageSummaryQuery(DateOnly? From, DateOnly? To, Guid? UserId = null)
    : IQuery<Result<AdminAiUsageSummaryModel>>;
