using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Dietologist.Models;

namespace FoodDiary.Application.Dietologist.Queries.GetInvitationByToken;

public record GetInvitationByTokenQuery(Guid? UserId, Guid InvitationId) : IQuery<Result<InvitationModel>>, IUserRequest;
