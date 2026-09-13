using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminBillingRevenueSummary;

public sealed record GetAdminBillingRevenueSummaryQuery(DateTime? FromUtc, DateTime? ToUtc)
    : IQuery<Result<AdminBillingRevenueSummaryReadModel>>;
