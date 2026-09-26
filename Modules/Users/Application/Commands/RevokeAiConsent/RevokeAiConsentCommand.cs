using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Commands.RevokeAiConsent;

public record RevokeAiConsentCommand(
    Guid? UserId
) : ICommand<Result>, IUserRequest;
