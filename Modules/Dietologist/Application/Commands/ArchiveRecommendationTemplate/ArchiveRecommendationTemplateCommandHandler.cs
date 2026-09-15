using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Commands.ArchiveRecommendationTemplate;

public sealed class ArchiveRecommendationTemplateCommandHandler(
    IRecommendationTemplateWriteRepository repository,
    ICurrentUserAccessService userContextService)
    : ICommandHandler<ArchiveRecommendationTemplateCommand, Result> {
    public async Task<Result> Handle(
        ArchiveRecommendationTemplateCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId, userContextService, cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        Result<RecommendationTemplateId> templateIdResult = DietologistRequiredIdParser.Parse(
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
            return Result.Failure(DietologistErrors.InvitationNotFound);
        }

        template.Archive();
        return Result.Success();
    }
}
