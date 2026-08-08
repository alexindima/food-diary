using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminBillingRevenueSummary;

public sealed record GetAdminBillingRevenueSummaryQuery(DateTime? FromUtc, DateTime? ToUtc)
    : IQuery<Result<AdminBillingRevenueSummaryReadModel>>;
