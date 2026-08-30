using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Domain.Entities.Achievements;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Gamification.Services;

public sealed class AchievementDefinitionAdministrationService(IAchievementDefinitionStore store)
    : IAchievementDefinitionAdministrationService {
    public async Task<IReadOnlyList<AchievementDefinitionAdminModel>> GetAllAsync(CancellationToken cancellationToken) =>
        (await store.GetAllAsync(cancellationToken).ConfigureAwait(false)).Select(ToModel).ToList();

    public async Task<Result<AchievementDefinitionAdminModel>> CreateAsync(
        AchievementDefinitionCreateInput input,
        CancellationToken cancellationToken) {
        Result<AchievementMetric> metric = ParseMetric(input.Metric);
        if (metric.IsFailure) {
            return Result.Failure<AchievementDefinitionAdminModel>(metric.Error);
        }

        try {
            var definition = AchievementDefinition.Create(
                input.Key, input.Category, metric.Value, input.Threshold, input.TitleRu, input.TitleEn,
                input.DescriptionRu, input.DescriptionEn, input.Icon, input.SortOrder, input.IsActive);
            bool added = await store.TryAddAsync(definition, cancellationToken).ConfigureAwait(false);
            if (!added) {
                return Result.Failure<AchievementDefinitionAdminModel>(AchievementDefinitionErrors.KeyConflict(definition.Key));
            }
            return Result.Success(ToModel(definition));
        } catch (ArgumentException exception) {
            return Result.Failure<AchievementDefinitionAdminModel>(Errors.Validation.Invalid("definition", exception.Message));
        }
    }

    public async Task<Result<AchievementDefinitionAdminModel>> UpdateAsync(
        Guid id,
        AchievementDefinitionUpdateInput input,
        CancellationToken cancellationToken) {
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
        Enum.TryParse(value, ignoreCase: true, out AchievementMetric metric)
            ? Result.Success(metric)
            : Result.Failure<AchievementMetric>(
                Errors.Validation.Invalid("metric", "Unsupported achievement metric."));

    private static AchievementDefinitionAdminModel ToModel(AchievementDefinition definition) => new(
        definition.Id.Value, definition.Key, definition.Category, definition.Metric.ToString(), definition.Threshold,
        definition.TitleRu, definition.TitleEn, definition.DescriptionRu, definition.DescriptionEn, definition.Icon,
        definition.SortOrder, definition.IsActive, definition.Version);
}
