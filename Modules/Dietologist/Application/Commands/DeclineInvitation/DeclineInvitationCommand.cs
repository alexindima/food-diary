using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Commands.DeclineInvitation;

public record DeclineInvitationCommand(
    Guid InvitationId,
    string Token,
    Guid? UserId) : ICommand<Result>, IUserRequest;
