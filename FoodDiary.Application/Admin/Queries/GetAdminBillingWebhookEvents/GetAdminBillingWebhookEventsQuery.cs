using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminBillingWebhookEvents;

public sealed record GetAdminBillingWebhookEventsQuery(
    int Page,
    int Limit,
    string? Provider,
    string? Status,
    string? Search,
    DateTime? FromUtc,
    DateTime? ToUtc)
    : IQuery<Result<PagedResponse<AdminBillingWebhookEventReadModel>>>;
