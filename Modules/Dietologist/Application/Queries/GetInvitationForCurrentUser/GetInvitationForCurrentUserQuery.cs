using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Models;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetInvitationForCurrentUser;

public sealed record GetInvitationForCurrentUserQuery(Guid? UserId, Guid InvitationId)
    : IQuery<Result<DietologistInvitationForCurrentUserModel>>, IUserRequest;
