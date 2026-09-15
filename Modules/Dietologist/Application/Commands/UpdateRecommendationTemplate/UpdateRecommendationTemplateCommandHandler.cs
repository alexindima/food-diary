using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Application.Common.Validation;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Commands.UpdateRecommendationTemplate;

public sealed class UpdateRecommendationTemplateCommandHandler(
    IRecommendationTemplateWriteRepository repository,
    ICurrentUserAccessService userContextService)
    : ICommandHandler<UpdateRecommendationTemplateCommand, Result<RecommendationTemplateModel>> {
    public async Task<Result<RecommendationTemplateModel>> Handle(
        UpdateRecommendationTemplateCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId, userContextService, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<RecommendationTemplateModel>(userIdResult);
        }

        Result<RecommendationTemplateId> templateIdResult = DietologistRequiredIdParser.Parse(
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
            return Result.Failure<RecommendationTemplateModel>(DietologistErrors.InvitationNotFound);
        }

        template.Update(command.Name, command.Text);
        return Result.Success(template.ToModel());
    }
}
