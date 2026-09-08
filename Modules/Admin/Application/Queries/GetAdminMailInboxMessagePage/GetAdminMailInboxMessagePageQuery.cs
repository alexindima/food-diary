using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessagePage;

public sealed record GetAdminMailInboxMessagePageQuery(int Page = 1, int Limit = 50, string? Recipient = null, string? Category = null, bool? Unread = null, DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null, string? Search = null, string? FromAddress = null, Guid? Id = null)
    : IQuery<Result<AdminMailInboxMessagePageModel>>;

