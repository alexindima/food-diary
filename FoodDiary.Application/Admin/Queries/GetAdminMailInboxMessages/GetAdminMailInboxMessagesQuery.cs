using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessages;

public sealed record GetAdminMailInboxMessagesQuery(int Limit)
    : IQuery<Result<IReadOnlyList<AdminMailInboxMessageSummaryModel>>>;
