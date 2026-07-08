using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.RevokeInvitation;

public record RevokeInvitationCommand(Guid? UserId) : ICommand<Result>, IUserRequest;
