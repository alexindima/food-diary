using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminUserPremiumGrant;

public sealed record GetAdminUserPremiumGrantQuery(Guid UserId) : IQuery<Result<UserManualPremiumGrantModel>>;
