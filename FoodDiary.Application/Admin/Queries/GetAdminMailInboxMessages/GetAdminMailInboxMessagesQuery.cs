using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessages;

public sealed record GetAdminMailInboxMessagesQuery(int Limit)
    : IQuery<Result<IReadOnlyList<AdminMailInboxMessageSummaryModel>>>;
