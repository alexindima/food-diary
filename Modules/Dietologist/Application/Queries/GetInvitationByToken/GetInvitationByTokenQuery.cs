using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Models;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetInvitationByToken;

public record GetInvitationByTokenQuery(Guid? UserId, Guid InvitationId) : IQuery<Result<InvitationModel>>, IUserRequest;
