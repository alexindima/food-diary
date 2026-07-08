using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.DeclineInvitation;

public record DeclineInvitationCommand(
    Guid InvitationId,
    string Token,
    Guid? UserId) : ICommand<Result>, IUserRequest;
