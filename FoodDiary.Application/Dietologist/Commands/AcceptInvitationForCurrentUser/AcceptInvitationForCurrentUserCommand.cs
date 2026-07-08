using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.AcceptInvitationForCurrentUser;

public sealed record AcceptInvitationForCurrentUserCommand(Guid? UserId, Guid InvitationId) : ICommand<Result>, IUserRequest;
