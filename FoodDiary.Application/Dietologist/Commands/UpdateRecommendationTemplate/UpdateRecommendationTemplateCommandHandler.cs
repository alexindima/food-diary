using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.UpdateRecommendationTemplate;

public sealed class UpdateRecommendationTemplateCommandHandler(
    IRecommendationTemplateRepository repository,
    IUserContextService userContextService)
    : ICommandHandler<UpdateRecommendationTemplateCommand, Result<RecommendationTemplateModel>> {
    public async Task<Result<RecommendationTemplateModel>> Handle(
        UpdateRecommendationTemplateCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId, userContextService, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<RecommendationTemplateModel>(userIdResult);
        }

        Result<RecommendationTemplateId> templateIdResult = RequiredIdParser.Parse(
            command.TemplateId,
            nameof(command.TemplateId),
            "Template id must not be empty.",
            value => new RecommendationTemplateId(value));
        if (templateIdResult.IsFailure) {
            return Result.Failure<RecommendationTemplateModel>(templateIdResult.Error);
        }

        RecommendationTemplate? template = await repository.GetByIdAsync(
            templateIdResult.Value, asTracking: true, cancellationToken).ConfigureAwait(false);
        if (template is null || template.DietologistUserId != userIdResult.Value) {
            return Result.Failure<RecommendationTemplateModel>(Errors.Dietologist.InvitationNotFound);
        }

        template.Update(command.Name, command.Text);
        return Result.Success(template.ToModel());
    }
}
