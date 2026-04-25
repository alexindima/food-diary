using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessageDetails;

public sealed record GetAdminMailInboxMessageDetailsQuery(Guid Id)
    : IQuery<Result<AdminMailInboxMessageDetailsModel>>;
