using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminBillingCancellations;

public sealed record GetAdminBillingCancellationsQuery(int Page, int Limit, string? Provider, string? Status,
    string? Search, DateTime? FromUtc, DateTime? ToUtc) : IQuery<Result<AdminBillingCancellationReportReadModel>>;
