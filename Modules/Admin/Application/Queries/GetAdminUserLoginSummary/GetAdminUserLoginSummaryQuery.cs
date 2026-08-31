using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminUserLoginSummary;

public sealed record GetAdminUserLoginSummaryQuery(
    DateTime? FromUtc,
    DateTime? ToUtc) : IQuery<Result<IReadOnlyList<AdminUserLoginDeviceSummaryModel>>>;
