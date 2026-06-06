using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Users.Commands.AcceptAiConsent;

public record AcceptAiConsentCommand(
    Guid? UserId
) : ICommand<Result>, IUserRequest;
