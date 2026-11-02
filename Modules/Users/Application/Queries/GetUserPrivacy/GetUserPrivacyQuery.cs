using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Queries.GetUserPrivacy;

public sealed record GetUserPrivacyQuery(Guid? UserId) : IQuery<Result<UserPrivacyStateModel>>, IUserRequest;
