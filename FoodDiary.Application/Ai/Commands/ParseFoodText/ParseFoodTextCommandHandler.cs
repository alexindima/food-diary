using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Commands.ParseFoodText;

public sealed class ParseFoodTextCommandHandler(
    IOpenAiFoodService openAiFoodService,
    IAiUserContextService aiUserContextService)
    : ICommandHandler<ParseFoodTextCommand, Result<FoodVisionModel>> {
    public async Task<Result<FoodVisionModel>> Handle(
        ParseFoodTextCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<FoodVisionModel>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Result<AiUserContext> contextResult = await aiUserContextService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return Result.Failure<FoodVisionModel>(contextResult.Error);
        }

        return await openAiFoodService.ParseFoodTextAsync(
            command.Text,
            contextResult.Value.Language,
            userId,
            cancellationToken).ConfigureAwait(false);
    }
}
