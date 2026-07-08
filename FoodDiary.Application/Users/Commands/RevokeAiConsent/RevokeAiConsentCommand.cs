using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Users.Commands.RevokeAiConsent;

public record RevokeAiConsentCommand(
    Guid? UserId
) : ICommand<Result>, IUserRequest;
