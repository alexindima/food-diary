using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Users.Commands.RevokeAiConsent;

public record RevokeAiConsentCommand(
    Guid? UserId
) : ICommand<Result>, IUserRequest;
