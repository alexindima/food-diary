using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Commands.ParseFoodText;

public sealed class ParseFoodTextCommandHandler(
    IOpenAiFoodService openAiFoodService,
    IAiUserContextService aiUserContextService,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<ParseFoodTextCommand, Result<FoodVisionModel>> {
    public async Task<Result<FoodVisionModel>> Handle(
        ParseFoodTextCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<FoodVisionModel>(userIdResult);
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
