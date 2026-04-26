using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Dietologist.Models;

namespace FoodDiary.Application.Dietologist.Queries.GetInvitationByToken;

public record GetInvitationByTokenQuery(Guid InvitationId) : IQuery<Result<InvitationModel>>;
