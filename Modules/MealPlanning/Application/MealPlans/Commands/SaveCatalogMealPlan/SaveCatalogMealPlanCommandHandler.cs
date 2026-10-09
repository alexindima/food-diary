using FoodDiary.Modules.MealPlanning.Domain.ValueObjects;
using FoodDiary.Application.Contracts.Common.Validation;
using FoodDiary.Modules.MealPlanning.Application.Common.Validation;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Mappings;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Models;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.SaveCatalogMealPlan;

public sealed class SaveCatalogMealPlanCommandHandler(IMealPlanCatalogRepository catalog,
    IMealPlanWriteRepository repository, IMealPlanCatalogRecipeReader recipes,
    IMealPlanCompositionReader composition) : ICommandHandler<SaveCatalogMealPlanCommand, Result<MealPlanModel>> {
    public async Task<Result<MealPlanModel>> Handle(SaveCatalogMealPlanCommand command, CancellationToken cancellationToken) {
        FluentValidation.Results.ValidationResult validation = await new SaveCatalogMealPlanCommandValidator().ValidateAsync(command, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid) {
            return Result.Failure<MealPlanModel>(new Error("MealPlan.InvalidCatalogPlan",
                string.Join(' ', validation.Errors.Select(error => error.ErrorMessage).Distinct(StringComparer.Ordinal)), Kind: ErrorKind.Validation));
        }

        Guid[] recipeIds = [.. command.Days.SelectMany(day => day.Meals).Select(meal => meal.RecipeId).Distinct()];
        IReadOnlySet<Guid> publicIds = await recipes.GetPublicIdsAsync(recipeIds, cancellationToken).ConfigureAwait(false);
        if (recipeIds.Any(id => !publicIds.Contains(id))) {
            return Result.Failure<MealPlanModel>(new Error("MealPlan.RecipeUnavailable",
                "Select existing public recipes. Private or missing recipes cannot be used in catalog plans.", Kind: ErrorKind.Validation));
        }

        Guid planGuid = command.Id ?? Guid.Empty;
        MealPlanId planId = command.Id.HasValue
            ? RequiredIdParser.Parse(planGuid, nameof(command.Id), "Meal plan id is required.", value => new MealPlanId(value)).Value
            : MealPlanId.Empty;
        DietType dietType = SharedEnumValueParser.ParseRequired<DietType>(command.DietType, nameof(command.DietType), "Invalid diet type.").Value;
        MealPlan? plan = command.Id.HasValue
            ? await catalog.GetForUpdateAsync(planId, cancellationToken).ConfigureAwait(false)
            : MealPlan.CreateCuratedWithDuration(command.Name, command.Description, dietType, PlanDurationDays.FromDays(command.DurationDays), command.TargetCaloriesPerDay);
        if (plan is null) { return Result.Failure<MealPlanModel>(MealPlanErrors.NotFound(command.Id!.Value)); }

        plan.UpdateCatalogWithDuration(command.Name, command.Description, dietType,
            PlanDurationDays.FromDays(command.DurationDays), command.TargetCaloriesPerDay, command.IsPublished);
        IReadOnlyDictionary<RecipeId, MealPlanRecipeSnapshot> snapshots = await composition.GetRecipeSnapshotsAsync([.. recipeIds.Select(id => new RecipeId(id))], cancellationToken).ConfigureAwait(false);
        foreach (CatalogDayInput input in command.Days.OrderBy(day => day.DayNumber)) {
            MealPlanDay day = plan.AddTypedDay(PlanDayNumber.FromIndex(input.DayNumber));
            foreach (CatalogMealInput meal in input.Meals) {
                day.AddMealWithServings(SharedEnumValueParser.ParseRequired<MealType>(meal.MealType, nameof(meal.MealType), "Invalid meal type.").Value, new RecipeId(meal.RecipeId), PlannedServings.FromCount(meal.Servings))
                    .SetRecipeSnapshot(snapshots.GetValueOrDefault(new RecipeId(meal.RecipeId)));
            }
        }

        if (!command.Id.HasValue) { await repository.AddAsync(plan, cancellationToken).ConfigureAwait(false); }
        return Result.Success(plan.ToModel());
    }
}
