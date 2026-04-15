using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Dietologist.Commands.DeclineInvitationForCurrentUser;

public sealed record DeclineInvitationForCurrentUserCommand(Guid? UserId, Guid InvitationId) : ICommand<Result>, IUserRequest;
