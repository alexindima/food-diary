using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Users.Commands.RevokeAiConsent;

public record RevokeAiConsentCommand(
    Guid? UserId
) : ICommand<Result>, IUserRequest;
