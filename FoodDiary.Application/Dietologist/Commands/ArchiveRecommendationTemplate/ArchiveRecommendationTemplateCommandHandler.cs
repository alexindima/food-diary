using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.ArchiveRecommendationTemplate;

public sealed class ArchiveRecommendationTemplateCommandHandler(
    IRecommendationTemplateRepository repository,
    IUserContextService userContextService)
    : ICommandHandler<ArchiveRecommendationTemplateCommand, Result> {
    public async Task<Result> Handle(
        ArchiveRecommendationTemplateCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId, userContextService, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        Result<RecommendationTemplateId> templateIdResult = RequiredIdParser.Parse(
            command.TemplateId,
            nameof(command.TemplateId),
            "Template id must not be empty.",
            value => new RecommendationTemplateId(value));
        if (templateIdResult.IsFailure) {
            return Result.Failure(templateIdResult.Error);
        }

        RecommendationTemplate? template = await repository.GetByIdAsync(
            templateIdResult.Value, asTracking: true, cancellationToken).ConfigureAwait(false);
        if (template is null || template.DietologistUserId != userIdResult.Value) {
            return Result.Failure(Errors.Dietologist.InvitationNotFound);
        }

        template.Archive();
        return Result.Success();
    }
}
