using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Dietologist.Commands.RevokeInvitation;

public record RevokeInvitationCommand(Guid? UserId) : ICommand<Result>, IUserRequest;
