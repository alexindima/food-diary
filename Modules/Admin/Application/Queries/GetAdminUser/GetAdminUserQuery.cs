using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Results;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminUser;

public sealed record GetAdminUserQuery(Guid UserId) : IQuery<Result<AdminUserModel>>;
