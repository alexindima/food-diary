using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Commands.AcceptInvitationForCurrentUser;

public sealed record AcceptInvitationForCurrentUserCommand(Guid? UserId, Guid InvitationId) : ICommand<Result>, IUserRequest;
