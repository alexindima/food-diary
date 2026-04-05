using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Ai.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;

namespace FoodDiary.Application.Ai.Commands.ParseFoodText;

public class ParseFoodTextCommandHandler(
    IOpenAiFoodService openAiFoodService,
    IUserRepository userRepository)
    : ICommandHandler<ParseFoodTextCommand, Result<FoodVisionModel>> {
    public async Task<Result<FoodVisionModel>> Handle(
        ParseFoodTextCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<FoodVisionModel>(userIdResult.Error);
        }

        var userId = userIdResult.Value;
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<FoodVisionModel>(accessError);
        }

        return await openAiFoodService.ParseFoodTextAsync(
            command.Text,
            user!.Language,
            userId,
            cancellationToken);
    }
}
