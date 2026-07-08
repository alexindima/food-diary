using FoodDiary.Application.Admin.Models;
using FoodDiary.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Admin.Queries.GetAdminUser;

public sealed record GetAdminUserQuery(Guid UserId) : IQuery<Result<AdminUserModel>>;
