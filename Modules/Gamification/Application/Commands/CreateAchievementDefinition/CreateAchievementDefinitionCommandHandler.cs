using FoodDiary.Modules.Gamification.Domain.Contracts.Enums;
using FoodDiary.Modules.Gamification.Contracts.Commands.CreateAchievementDefinition;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.Gamification.Contracts.Models;
using FoodDiary.Modules.Gamification.Domain.Entities.Achievements;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Gamification.Application.Commands.CreateAchievementDefinition;

public sealed class CreateAchievementDefinitionCommandHandler(IAchievementDefinitionStore store) : IRequestHandler<CreateAchievementDefinitionCommand, Result<AchievementDefinitionAdminModel>> {
    public async Task<Result<AchievementDefinitionAdminModel>> Handle(CreateAchievementDefinitionCommand request, CancellationToken cancellationToken) {
        AchievementDefinitionCreateInput input = request.Input;
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
