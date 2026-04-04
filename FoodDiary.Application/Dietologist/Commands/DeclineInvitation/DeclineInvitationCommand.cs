using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Dietologist.Commands.DeclineInvitation;

public record DeclineInvitationCommand(
    Guid InvitationId,
    string Token,
    Guid? UserId) : ICommand<Result>, IUserRequest;
