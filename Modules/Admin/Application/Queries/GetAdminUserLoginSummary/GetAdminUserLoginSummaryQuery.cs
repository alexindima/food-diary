using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminUserLoginSummary;

public sealed record GetAdminUserLoginSummaryQuery(
    DateTime? FromUtc,
    DateTime? ToUtc) : IQuery<Result<IReadOnlyList<AdminUserLoginDeviceSummaryModel>>>;
