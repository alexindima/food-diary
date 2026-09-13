using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminMailInboxMessages;

public sealed record GetAdminMailInboxMessagesQuery(int Limit, string? Recipient = null, string? Category = null, bool? Unread = null)
    : IQuery<Result<IReadOnlyList<AdminMailInboxMessageSummaryModel>>>;
