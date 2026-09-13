using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessageDetails;

public sealed record GetAdminMailInboxMessageDetailsQuery(Guid Id)
    : IQuery<Result<AdminMailInboxMessageDetailsModel>>;
