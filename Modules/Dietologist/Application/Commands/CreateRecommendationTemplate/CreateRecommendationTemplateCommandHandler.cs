using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Commands.CreateRecommendationTemplate;

public sealed class CreateRecommendationTemplateCommandHandler(
    IRecommendationTemplateWriteRepository repository,
    ICurrentUserAccessService userContextService)
    : ICommandHandler<CreateRecommendationTemplateCommand, Result<RecommendationTemplateModel>> {
    public async Task<Result<RecommendationTemplateModel>> Handle(
        CreateRecommendationTemplateCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId, userContextService, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<RecommendationTemplateModel>(userIdResult);
        }

        var template = RecommendationTemplate.Create(userIdResult.Value, command.Name, command.Text);
        await repository.AddAsync(template, cancellationToken).ConfigureAwait(false);
        return Result.Success(template.ToModel());
    }
}
