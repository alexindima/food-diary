using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminBillingWebhookEvents;

public sealed record GetAdminBillingWebhookEventsQuery(
    int Page,
    int Limit,
    string? Provider,
    string? Status,
    string? Search,
    DateTime? FromUtc,
    DateTime? ToUtc)
    : IQuery<Result<PagedResponse<AdminBillingWebhookEventReadModel>>>;
