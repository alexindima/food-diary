using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Dietologist.Models;

namespace FoodDiary.Application.Dietologist.Queries.GetInvitationForCurrentUser;

public sealed record GetInvitationForCurrentUserQuery(Guid? UserId, Guid InvitationId)
    : IQuery<Result<DietologistInvitationForCurrentUserModel>>, IUserRequest;
