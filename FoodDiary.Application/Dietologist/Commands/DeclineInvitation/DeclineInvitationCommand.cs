using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Dietologist.Commands.DeclineInvitation;

public record DeclineInvitationCommand(
    Guid InvitationId,
    string Token,
    Guid? UserId) : ICommand<Result>, IUserRequest;
