using FoodDiary.Modules.Gamification.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Gamification.Domain.Contracts.Enums;
using FoodDiary.Modules.Gamification.Contracts.Commands.UpdateAchievementDefinition;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.Gamification.Contracts.Models;
using FoodDiary.Modules.Gamification.Domain.Entities.Achievements;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Gamification.Application.Commands.UpdateAchievementDefinition;

public sealed class UpdateAchievementDefinitionCommandHandler(IAchievementDefinitionStore store) : IRequestHandler<UpdateAchievementDefinitionCommand, Result<AchievementDefinitionAdminModel>> {
    public async Task<Result<AchievementDefinitionAdminModel>> Handle(UpdateAchievementDefinitionCommand request, CancellationToken cancellationToken) {
        Guid id = request.Id;
        AchievementDefinitionUpdateInput input = request.Input;
        var definitionId = new AchievementDefinitionId(id);
        AchievementDefinition? definition = await store.GetByIdTrackingAsync(definitionId, cancellationToken).ConfigureAwait(false);
        if (definition is null) {
            return Result.Failure<AchievementDefinitionAdminModel>(AchievementDefinitionErrors.NotFound(id));
        }

        if (definition.Version != input.Version) {
            return Result.Failure<AchievementDefinitionAdminModel>(AchievementDefinitionErrors.VersionConflict());
        }

        Result<AchievementMetric> metric = ParseMetric(input.Metric);
        if (metric.IsFailure) {
            return Result.Failure<AchievementDefinitionAdminModel>(metric.Error);
        }

        try {
            definition.Update(
                input.Category, metric.Value, input.Threshold, input.TitleRu, input.TitleEn,
                input.DescriptionRu, input.DescriptionEn, input.Icon, input.SortOrder, input.IsActive);
            await store.UpdateAsync(definition, cancellationToken).ConfigureAwait(false);
            return Result.Success(ToModel(definition));
        } catch (ArgumentException exception) {
            return Result.Failure<AchievementDefinitionAdminModel>(Errors.Validation.Invalid("definition", exception.Message));
        }

    }

    private static Result<AchievementMetric> ParseMetric(string value) =>
        SharedEnumValueParser.TryParse(value, out AchievementMetric metric)
            ? Result.Success(metric)
            : Result.Failure<AchievementMetric>(
                Errors.Validation.Invalid("metric", "Unsupported achievement metric."));

    private static AchievementDefinitionAdminModel ToModel(AchievementDefinition definition) => new(
        definition.Id.Value, definition.Key, definition.Category, definition.Metric.ToString(), definition.Threshold,
        definition.TitleRu, definition.TitleEn, definition.DescriptionRu, definition.DescriptionEn, definition.Icon,
        definition.SortOrder, definition.IsActive, definition.Version);

}
