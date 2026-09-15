using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Commands.AcceptAiConsent;

public record AcceptAiConsentCommand(
    Guid? UserId
) : ICommand<Result>, IUserRequest;
