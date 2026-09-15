using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Commands.DeclineInvitationForCurrentUser;

public sealed record DeclineInvitationForCurrentUserCommand(Guid? UserId, Guid InvitationId) : ICommand<Result>, IUserRequest;
