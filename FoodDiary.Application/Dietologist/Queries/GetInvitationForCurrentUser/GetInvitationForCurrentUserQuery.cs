using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Dietologist.Models;

namespace FoodDiary.Application.Dietologist.Queries.GetInvitationForCurrentUser;

public sealed record GetInvitationForCurrentUserQuery(Guid? UserId, Guid InvitationId)
    : IQuery<Result<DietologistInvitationForCurrentUserModel>>, IUserRequest;
