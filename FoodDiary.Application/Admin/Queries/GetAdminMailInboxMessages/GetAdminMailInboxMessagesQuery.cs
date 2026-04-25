using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessages;

public sealed record GetAdminMailInboxMessagesQuery(int Limit)
    : IQuery<Result<IReadOnlyList<AdminMailInboxMessageSummaryModel>>>;
