using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminMailInboxMessageDetails;

public sealed record GetAdminMailInboxMessageDetailsQuery(Guid Id)
    : IQuery<Result<AdminMailInboxMessageDetailsModel>>;
