using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;

using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;

namespace FoodDiary.Modules.Ai.Application.Commands.ParseFoodText;

public sealed class ParseFoodTextCommandHandler(
    IOpenAiFoodService openAiFoodService,
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
        return await openAiFoodService.ParseFoodTextAsync(
            command.Text,
            userId,
            command.RequestId,
            cancellationToken).ConfigureAwait(false);
    }
}
