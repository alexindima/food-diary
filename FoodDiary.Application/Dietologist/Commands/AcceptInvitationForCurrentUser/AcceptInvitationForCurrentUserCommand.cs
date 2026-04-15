using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Dietologist.Commands.AcceptInvitationForCurrentUser;

public sealed record AcceptInvitationForCurrentUserCommand(Guid? UserId, Guid InvitationId) : ICommand<Result>, IUserRequest;
