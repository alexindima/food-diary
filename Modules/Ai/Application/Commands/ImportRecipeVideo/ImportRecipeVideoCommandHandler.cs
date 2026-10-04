using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Ai.Application.Commands.ImportRecipeVideo;

public sealed class ImportRecipeVideoCommandHandler(IOpenAiFoodService service, ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<ImportRecipeVideoCommand, Result<RecipeImportDraftModel>> {
    public async Task<Result<RecipeImportDraftModel>> Handle(ImportRecipeVideoCommand command, CancellationToken cancellationToken) {
        Result<UserId> user = await CurrentUserAccessResolver.ResolveAsync(command.UserId, currentUserAccessService, cancellationToken).ConfigureAwait(false);
        if (user.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<RecipeImportDraftModel>(user);
        }
        return await service.ImportRecipeVideoAsync(command.Video, command.SourceUrl, command.Text, user.Value, command.RequestId, cancellationToken).ConfigureAwait(false);
    }
}
