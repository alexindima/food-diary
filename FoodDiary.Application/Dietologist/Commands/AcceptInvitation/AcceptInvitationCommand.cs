using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Dietologist.Commands.AcceptInvitation;

public record AcceptInvitationCommand(
    Guid InvitationId,
    string Token,
    Guid? UserId) : ICommand<Result>, IUserRequest;
