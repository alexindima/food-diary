using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Common.Validation;
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

namespace FoodDiary.Application.Consumptions.Commands.CreateConsumption;

public class CreateConsumptionCommandHandler(
    IMealRepository mealRepository,
    IMealNutritionService mealNutritionService,
    IRecentItemRepository recentItemRepository,
    IUserRepository userRepository,
    TimeProvider dateTimeProvider,
    IImageAssetAccessService imageAssetAccessService)
    : ICommandHandler<CreateConsumptionCommand, Result<ConsumptionModel>> {
    private sealed record CreateConsumptionValues(
        UserId UserId,
        MealType? MealType,
        ImageAssetId? ImageAssetId,
        ImageAsset? ImageAsset);

    public async Task<Result<ConsumptionModel>> Handle(CreateConsumptionCommand command, CancellationToken cancellationToken) {
        Result<CreateConsumptionValues> valuesResult = await PrepareCreateValuesAsync(command, cancellationToken).ConfigureAwait(false);
        if (valuesResult.IsFailure) {
            return Result.Failure<ConsumptionModel>(valuesResult.Error);
        }

        CreateConsumptionValues values = valuesResult.Value;
        var meal = Meal.Create(
            values.UserId,
            command.Date,
            values.MealType,
            command.Comment,
            values.ImageAsset?.Url ?? command.ImageUrl,
            values.ImageAssetId);
        meal.UpdateSatietyLevels(command.PreMealSatietyLevel, command.PostMealSatietyLevel);

        Result itemsResult = AddManualItems(meal, command.Items);
        if (itemsResult.IsFailure) {
            return Result.Failure<ConsumptionModel>(itemsResult.Error);
        }

        Result aiSessionsResult = await AddAiSessionsAsync(meal, command.AiSessions, values.UserId, cancellationToken).ConfigureAwait(false);
        if (aiSessionsResult.IsFailure) {
            return Result.Failure<ConsumptionModel>(aiSessionsResult.Error);
        }

        Result nutritionResult = await ApplyNutritionAsync(meal, command, values.UserId, cancellationToken).ConfigureAwait(false);
        if (nutritionResult.IsFailure) {
            return Result.Failure<ConsumptionModel>(nutritionResult.Error);
        }

        return await SaveAndLoadAsync(meal, values.UserId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<CreateConsumptionValues>> PrepareCreateValuesAsync(
        CreateConsumptionCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<CreateConsumptionValues>(Errors.Authentication.InvalidToken);
        }

        Result<ImageAssetId?> imageAssetIdResult = ImageAssetIdParser.ParseOptional(command.ImageAssetId, nameof(command.ImageAssetId));
        if (imageAssetIdResult.IsFailure) {
            return Result.Failure<CreateConsumptionValues>(imageAssetIdResult.Error);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<CreateConsumptionValues>(accessError);
        }

        Result<ImageAsset?> imageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
            imageAssetIdResult.Value,
            userId,
            cancellationToken).ConfigureAwait(false);
        if (imageAssetResult.IsFailure) {
            return Result.Failure<CreateConsumptionValues>(imageAssetResult.Error);
        }

        Result<MealType?> mealTypeResult = EnumValueParser.ParseOptional<MealType>(
            command.MealType,
            nameof(command.MealType),
            "Unknown meal type value.");
        if (mealTypeResult.IsFailure) {
            return Result.Failure<CreateConsumptionValues>(mealTypeResult.Error);
        }

        return Result.Success(new CreateConsumptionValues(
            userId,
            mealTypeResult.Value,
            imageAssetIdResult.Value,
            imageAssetResult.Value));
    }

    private static Result AddManualItems(Meal meal, IEnumerable<ConsumptionItemInput> items) {
        foreach (ConsumptionItemInput item in items) {
            Result validation = ConsumptionItemValidator.Validate(item);
            if (validation.IsFailure) {
                return validation;
            }

            Result itemIdValidation = ValidateItemIdentifiers(item);
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
        foreach (ConsumptionAiSessionInput session in sessions) {
            Result sessionResult = await AddAiSessionAsync(meal, session, userId, cancellationToken).ConfigureAwait(false);
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
        Result<ImageAssetId?> sessionImageAssetIdResult = ImageAssetIdParser.ParseOptional(session.ImageAssetId, nameof(session.ImageAssetId));
        if (sessionImageAssetIdResult.IsFailure) {
            return sessionImageAssetIdResult;
        }

        Result<ImageAsset?> sessionImageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
            sessionImageAssetIdResult.Value,
            userId,
            cancellationToken).ConfigureAwait(false);
        if (sessionImageAssetResult.IsFailure) {
            return sessionImageAssetResult;
        }

        Result<List<MealAiItemData>> sessionItemsResult = CreateAiSessionItems(session);
        if (sessionItemsResult.IsFailure) {
            return sessionItemsResult;
        }

        if (!TryParseAiRecognitionSource(session.Source, out AiRecognitionSource sessionSource)) {
            return Result.Failure(
                    Errors.Validation.Invalid(nameof(session.Source), "Unknown AI recognition source value."));
        }

        DateTime recognizedAtUtc = session.RecognizedAtUtc ?? dateTimeProvider.GetUtcNow().UtcDateTime;
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
        CreateConsumptionCommand command,
        UserId userId,
        CancellationToken cancellationToken) {
        if (command.IsNutritionAutoCalculated) {
            Result<MealNutritionSummary> nutritionResult = await mealNutritionService.CalculateAsync(meal, userId, cancellationToken).ConfigureAwait(false);
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

    private static Result ApplyManualNutrition(Meal meal, CreateConsumptionCommand command) {
        Result<ManualNutritionInput> manualNutritionResult = ManualNutritionValidator.Validate(
            command.ManualCalories,
            command.ManualProteins,
            command.ManualFats,
            command.ManualCarbs,
            command.ManualFiber,
            command.ManualAlcohol);
        if (manualNutritionResult.IsFailure) {
            return manualNutritionResult;
        }

        ManualNutritionInput manual = manualNutritionResult.Value;
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

    private async Task<Result<ConsumptionModel>> SaveAndLoadAsync(
        Meal meal,
        UserId userId,
        CancellationToken cancellationToken) {
        await mealRepository.AddAsync(meal, cancellationToken).ConfigureAwait(false);
        await recentItemRepository.RegisterUsageAsync(
            userId,
            meal.Items.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).ToList(),
            meal.Items.Where(x => x.RecipeId.HasValue).Select(x => x.RecipeId!.Value).ToList(),
            cancellationToken).ConfigureAwait(false);

        Meal? created = await mealRepository.GetByIdAsync(
            meal.Id,
            userId,
            includeItems: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return created is null
            ? Result.Failure<ConsumptionModel>(Errors.Consumption.InvalidData("Failed to load created consumption."))
            : Result.Success(created.ToModel());
    }

    private static Result ValidateItemIdentifiers(ConsumptionItemInput item) {
        Result productIdResult = OptionalEntityIdValidator.EnsureNotEmpty(item.ProductId, nameof(item.ProductId), "Product id");
        if (productIdResult.IsFailure) {
            return productIdResult;
        }

        Result recipeIdResult = OptionalEntityIdValidator.EnsureNotEmpty(item.RecipeId, nameof(item.RecipeId), "Recipe id");
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
        foreach (ConsumptionAiItemInput aiItem in session.Items) {
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
                    out MealAiItemData? data,
                    out string? error)) {
                return Result.Failure<List<MealAiItemData>>(
                    Errors.Validation.Invalid("AiSessions", error ?? "AI item is invalid."));
            }

            items.Add(data!);
        }

        return Result.Success(items);
    }
}
