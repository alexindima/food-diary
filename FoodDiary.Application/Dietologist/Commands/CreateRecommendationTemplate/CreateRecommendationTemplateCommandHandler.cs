using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.CreateRecommendationTemplate;

public sealed class CreateRecommendationTemplateCommandHandler(
    IRecommendationTemplateRepository repository,
    IUserContextService userContextService)
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
