using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Commands.RevokeInvitation;

public record RevokeInvitationCommand(Guid? UserId) : ICommand<Result>, IUserRequest;
