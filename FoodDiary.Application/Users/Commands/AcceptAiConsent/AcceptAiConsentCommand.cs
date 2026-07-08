using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Users.Commands.AcceptAiConsent;

public record AcceptAiConsentCommand(
    Guid? UserId
) : ICommand<Result>, IUserRequest;
