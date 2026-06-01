using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Commands.UpdateConsumption;

public class UpdateConsumptionCommandHandler(
    IMealRepository mealRepository,
    IMealNutritionService mealNutritionService,
    IRecentItemRepository recentItemRepository,
    IImageAssetCleanupService imageAssetCleanupService,
    IUserRepository userRepository,
    IDateTimeProvider dateTimeProvider,
    IImageAssetAccessService imageAssetAccessService)
    : ICommandHandler<UpdateConsumptionCommand, Result<ConsumptionModel>> {
    private sealed record UpdateConsumptionValues(
        UserId UserId,
        MealId MealId,
        Meal Meal,
        MealType? MealType,
        ImageAssetId? ImageAssetId,
        ImageAsset? ImageAsset,
        ImageAssetId? OldAssetId);

    public async Task<Result<ConsumptionModel>> Handle(UpdateConsumptionCommand command, CancellationToken cancellationToken) {
        var valuesResult = await PrepareUpdateValuesAsync(command, cancellationToken).ConfigureAwait(false);
        if (valuesResult.IsFailure) {
            return Result.Failure<ConsumptionModel>(valuesResult.Error);
        }

        var values = valuesResult.Value;
        var updateResult = await ApplyUpdatesAsync(values.Meal, command, values, cancellationToken).ConfigureAwait(false);
        if (updateResult.IsFailure) return Result.Failure<ConsumptionModel>(updateResult.Error);

        await mealRepository.UpdateAsync(values.Meal, cancellationToken).ConfigureAwait(false);
        await recentItemRepository.RegisterUsageAsync(
            values.UserId,
            values.Meal.Items.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).ToList(),
            values.Meal.Items.Where(x => x.RecipeId.HasValue).Select(x => x.RecipeId!.Value).ToList(),
            cancellationToken).ConfigureAwait(false);

        await CleanupOldImageAssetAsync(command, values.OldAssetId, cancellationToken).ConfigureAwait(false);
        return await LoadUpdatedAsync(values.Meal.Id, values.UserId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<UpdateConsumptionValues>> PrepareUpdateValuesAsync(
        UpdateConsumptionCommand command,
        CancellationToken cancellationToken) {
        var commandValidation = ValidateCommand(command);
        if (commandValidation.IsFailure) {
            return Result.Failure<UpdateConsumptionValues>(commandValidation.Error);
        }

        var userId = new UserId(command.UserId!.Value);
        var mealResult = await ResolveMealAsync(command, userId, cancellationToken).ConfigureAwait(false);
        if (mealResult.IsFailure) return Result.Failure<UpdateConsumptionValues>(mealResult.Error);

        var mealTypeResult = EnumValueParser.ParseOptional<MealType>(
            command.MealType,
            nameof(command.MealType),
            "Unknown meal type value.");
        if (mealTypeResult.IsFailure) {
            return Result.Failure<UpdateConsumptionValues>(mealTypeResult.Error);
        }

        var imageAssetIdResult = ImageAssetIdParser.ParseOptional(command.ImageAssetId, nameof(command.ImageAssetId));
        if (imageAssetIdResult.IsFailure) {
            return Result.Failure<UpdateConsumptionValues>(imageAssetIdResult.Error);
        }

        var meal = mealResult.Value;
        var oldAssetId = meal.ImageAssetId;
        var imageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
            imageAssetIdResult.Value,
            userId,
            cancellationToken).ConfigureAwait(false);
        if (imageAssetResult.IsFailure) {
            return Result.Failure<UpdateConsumptionValues>(imageAssetResult.Error);
        }

        return Result.Success(new UpdateConsumptionValues(
            userId,
            new MealId(command.ConsumptionId),
            meal,
            mealTypeResult.Value,
            imageAssetIdResult.Value,
            imageAssetResult.Value,
            oldAssetId));
    }

    private static Result ValidateCommand(UpdateConsumptionCommand command) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        if (command.ConsumptionId == Guid.Empty) {
            return Result.Failure(
                Errors.Validation.Invalid(nameof(command.ConsumptionId), "Consumption id must not be empty."));
        }

        var hasManualItems = command.Items is { Count: > 0 };
        var hasAiItems = command.AiSessions is { Count: > 0 } && command.AiSessions.Any(session => session.Items.Count > 0);
        return hasManualItems || hasAiItems
            ? Result.Success()
            : Result.Failure(Errors.Validation.Required("Items"));
    }

    private async Task<Result<Meal>> ResolveMealAsync(
        UpdateConsumptionCommand command,
        UserId userId,
        CancellationToken cancellationToken) {
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<Meal>(accessError);
        }

        var consumptionId = new MealId(command.ConsumptionId);
        var meal = await mealRepository.GetByIdAsync(
            consumptionId,
            userId,
            includeItems: true,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return meal is null
            ? Result.Failure<Meal>(Errors.Consumption.NotFound(command.ConsumptionId))
            : Result.Success(meal);
    }

    private async Task<Result> ApplyUpdatesAsync(
        Meal meal,
        UpdateConsumptionCommand command,
        UpdateConsumptionValues values,
        CancellationToken cancellationToken) {
        meal.UpdateDate(command.Date);
        meal.UpdateMealType(values.MealType);
        meal.UpdateComment(command.Comment);
        meal.UpdateImage(values.ImageAsset?.Url ?? command.ImageUrl, values.ImageAssetId);

        var satietyValidation = SatietyLevelValidator.Validate(
            command.PreMealSatietyLevel,
            command.PostMealSatietyLevel);

        if (satietyValidation.IsFailure) {
            return satietyValidation;
        }

        meal.UpdateSatietyLevels(command.PreMealSatietyLevel, command.PostMealSatietyLevel);
        meal.ClearItems();
        meal.ClearAiSessions();

        var itemsResult = AddManualItems(meal, command.Items);
        if (itemsResult.IsFailure) return itemsResult;

        var aiSessionsResult = await AddAiSessionsAsync(meal, command.AiSessions, values.UserId, cancellationToken).ConfigureAwait(false);
        if (aiSessionsResult.IsFailure) return aiSessionsResult;

        return await ApplyNutritionAsync(meal, command, values.UserId, cancellationToken).ConfigureAwait(false);
    }

    private static Result AddManualItems(Meal meal, IEnumerable<ConsumptionItemInput> items) {
        foreach (var item in items) {
            var validation = ConsumptionItemValidator.Validate(item);
            if (validation.IsFailure) {
                return validation;
            }

            var itemIdValidation = ValidateItemIdentifiers(item);
            if (itemIdValidation.IsFailure) {
                return itemIdValidation;
            }

            if (item.ProductId.HasValue) {
                meal.AddProduct(new ProductId(item.ProductId.Value), item.Amount);
            } else if (item.RecipeId.HasValue) {
                meal.AddRecipe(new RecipeId(item.RecipeId.Value), item.Amount);
            }
        }

        return Result.Success();
    }

    private async Task<Result> AddAiSessionsAsync(
        Meal meal,
        IEnumerable<ConsumptionAiSessionInput> sessions,
        UserId userId,
        CancellationToken cancellationToken) {
        foreach (var session in sessions) {
            var sessionResult = await AddAiSessionAsync(meal, session, userId, cancellationToken).ConfigureAwait(false);
            if (sessionResult.IsFailure) {
                return sessionResult;
            }
        }

        return Result.Success();
    }

    private async Task<Result> AddAiSessionAsync(
        Meal meal,
        ConsumptionAiSessionInput session,
        UserId userId,
        CancellationToken cancellationToken) {
        var sessionImageAssetIdResult = ImageAssetIdParser.ParseOptional(session.ImageAssetId, nameof(session.ImageAssetId));
        if (sessionImageAssetIdResult.IsFailure) {
            return sessionImageAssetIdResult;
        }

        var sessionImageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
            sessionImageAssetIdResult.Value,
            userId,
            cancellationToken).ConfigureAwait(false);
        if (sessionImageAssetResult.IsFailure) {
            return sessionImageAssetResult;
        }

        var sessionItemsResult = CreateAiSessionItems(session);
        if (sessionItemsResult.IsFailure) {
            return sessionItemsResult;
        }

        if (!TryParseAiRecognitionSource(session.Source, out var sessionSource)) {
            return Result.Failure(
                    Errors.Validation.Invalid(nameof(session.Source), "Unknown AI recognition source value."));
        }

        var recognizedAtUtc = session.RecognizedAtUtc ?? dateTimeProvider.UtcNow;
        if (recognizedAtUtc.Kind == DateTimeKind.Unspecified) {
            return Result.Failure(
                    Errors.Validation.Invalid(nameof(session.RecognizedAtUtc), "RecognizedAtUtc timestamp kind must be specified."));
        }

        if (session.Notes is { Length: > 2048 }) {
            return Result.Failure(
                    Errors.Validation.Invalid(nameof(session.Notes), "Notes must be at most 2048 characters."));
        }

        meal.AddAiSession(
            sessionImageAssetIdResult.Value,
            sessionSource,
            recognizedAtUtc,
            session.Notes,
            sessionItemsResult.Value);

        return Result.Success();
    }

    private async Task<Result> ApplyNutritionAsync(
        Meal meal,
        UpdateConsumptionCommand command,
        UserId userId,
        CancellationToken cancellationToken) {
        if (command.IsNutritionAutoCalculated) {
            var nutritionResult = await mealNutritionService.CalculateAsync(meal, userId, cancellationToken).ConfigureAwait(false);
            if (nutritionResult.IsFailure) {
                return nutritionResult;
            }

            meal.ApplyNutrition(new MealNutritionUpdate(
                nutritionResult.Value.Calories,
                nutritionResult.Value.Proteins,
                nutritionResult.Value.Fats,
                nutritionResult.Value.Carbs,
                nutritionResult.Value.Fiber,
                nutritionResult.Value.Alcohol,
                IsAutoCalculated: true));
            return Result.Success();
        }

        return ApplyManualNutrition(meal, command);
    }

    private static Result ApplyManualNutrition(Meal meal, UpdateConsumptionCommand command) {
        var manualNutritionResult = ManualNutritionValidator.Validate(
            command.ManualCalories,
            command.ManualProteins,
            command.ManualFats,
            command.ManualCarbs,
            command.ManualFiber,
            command.ManualAlcohol);
        if (manualNutritionResult.IsFailure) {
            return manualNutritionResult;
        }

        var manual = manualNutritionResult.Value;
        meal.ApplyNutrition(new MealNutritionUpdate(
            manual.Calories,
            manual.Proteins,
            manual.Fats,
            manual.Carbs,
            manual.Fiber,
            manual.Alcohol,
            IsAutoCalculated: false,
            ManualCalories: manual.Calories,
            ManualProteins: manual.Proteins,
            ManualFats: manual.Fats,
            ManualCarbs: manual.Carbs,
            ManualFiber: manual.Fiber,
            ManualAlcohol: manual.Alcohol));
        return Result.Success();
    }

    private async Task CleanupOldImageAssetAsync(
        UpdateConsumptionCommand command,
        ImageAssetId? oldAssetId,
        CancellationToken cancellationToken) {
        var imageAssetChanged = command.ImageAssetId.HasValue &&
                                (!oldAssetId.HasValue || oldAssetId.Value.Value != command.ImageAssetId.Value);

        if (oldAssetId.HasValue && imageAssetChanged) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(oldAssetId.Value, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<Result<ConsumptionModel>> LoadUpdatedAsync(
        MealId mealId,
        UserId userId,
        CancellationToken cancellationToken) {
        var updated = await mealRepository.GetByIdAsync(
            mealId,
            userId,
            includeItems: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return updated is null
            ? Result.Failure<ConsumptionModel>(Errors.Consumption.InvalidData("Failed to load updated consumption."))
            : Result.Success(updated.ToModel());
    }

    private static Result ValidateItemIdentifiers(ConsumptionItemInput item) {
        var productIdResult = OptionalEntityIdValidator.EnsureNotEmpty(item.ProductId, nameof(item.ProductId), "Product id");
        if (productIdResult.IsFailure) {
            return productIdResult;
        }

        var recipeIdResult = OptionalEntityIdValidator.EnsureNotEmpty(item.RecipeId, nameof(item.RecipeId), "Recipe id");
        if (recipeIdResult.IsFailure) {
            return recipeIdResult;
        }

        return Result.Success();
    }

    private static bool TryParseAiRecognitionSource(string? source, out AiRecognitionSource result) {
        if (string.IsNullOrWhiteSpace(source)) {
            result = AiRecognitionSource.Text;
            return true;
        }

        return Enum.TryParse(source, true, out result);
    }

    private static Result<List<MealAiItemData>> CreateAiSessionItems(ConsumptionAiSessionInput session) {
        var items = new List<MealAiItemData>(session.Items.Count);
        foreach (var aiItem in session.Items) {
            if (!MealAiItemData.TryCreate(
                    aiItem.NameEn,
                    aiItem.NameLocal,
                    aiItem.Amount,
                    aiItem.Unit,
                    aiItem.Calories,
                    aiItem.Proteins,
                    aiItem.Fats,
                    aiItem.Carbs,
                    aiItem.Fiber,
                    aiItem.Alcohol,
                    out var data,
                    out var error)) {
                return Result.Failure<List<MealAiItemData>>(
                    Errors.Validation.Invalid("AiSessions", error ?? "AI item is invalid."));
            }

            items.Add(data!);
        }

        return Result.Success(items);
    }
}
