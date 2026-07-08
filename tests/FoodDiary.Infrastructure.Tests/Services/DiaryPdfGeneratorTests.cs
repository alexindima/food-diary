using System.Globalization;
using System.Net;
using System.Reflection;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Services.DiaryPdf;
using QuestPDF.Fluent;
using SkiaSharp;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class DiaryPdfGeneratorTests {
    [Fact]
    public async Task GenerateAsync_WithNoMeals_ReturnsPdfDocument() {
        var generator = new DiaryPdfGenerator();

        byte[] pdf = await generator.GenerateAsync(
            [],
            new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc),
            locale: "not-a-culture",
            timeZoneOffsetMinutes: null,
            reportOrigin: "not a host",
            CancellationToken.None);

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public async Task GenerateAsync_WithMeals_ReturnsPdfDocument() {
        var userId = UserId.New();
        Meal[] meals = [
            CreateMeal(userId, new DateTime(2026, 5, 2, 21, 4, 0, DateTimeKind.Utc), 946, 59, 45, 76, 7),
            CreateMeal(userId, new DateTime(2026, 5, 3, 20, 41, 0, DateTimeKind.Utc), 905, 58, 45, 66, 5),
            CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 41, 1, 0, 10, 3),
        ];
        var generator = new DiaryPdfGenerator();

        byte[] pdf = await generator.GenerateAsync(
            meals.Select(ToReadModel).ToArray(),
            new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            locale: null,
            240,
            "https://Ð´Ð½ÐµÐ²Ð½Ð¸ÐºÐµÐ´Ñ‹.Ñ€Ñ„",
            CancellationToken.None);

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public async Task GenerateAsync_WithMealImage_ReturnsPdfDocument() {
        const string transparentPngDataUrl =
            "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=";
        var userId = UserId.New();
        Meal meal = CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 41, 1, 0, 10, 3);
        meal.UpdateImage(transparentPngDataUrl);
        meal.UpdateSatietyLevels(2, 5);
        var generator = new DiaryPdfGenerator();

        byte[] pdf = await generator.GenerateAsync(
            [ToReadModel(meal)],
            new DateTime(2026, 5, 3, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            locale: null,
            240,
            "https://fooddiary.club",
            CancellationToken.None);

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public async Task GenerateAsync_WithMealImageAndComment_RendersImageAndCommentCardBranches() {
        const string transparentPngDataUrl =
            "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=";
        var userId = UserId.New();
        Meal meal = CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 41, 1, 0, 10, 3);
        meal.UpdateImage(transparentPngDataUrl);
        meal.UpdateComment("A meal comment");
        var generator = new DiaryPdfGenerator();

        byte[] pdf = await generator.GenerateAsync(
            [ToReadModel(meal)],
            new DateTime(2026, 5, 3, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            locale: null,
            240,
            "https://fooddiary.club",
            CancellationToken.None);

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public async Task GenerateAsync_WithCompositionRows_RendersDetailedMealCompositionTable() {
        var userId = UserId.New();
        Meal meal = CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 410, 12, 10, 40, 6);
        AddProductItem(meal, CreateProduct(userId, "Rice", imageUrl: ""), 150);
        var generator = new DiaryPdfGenerator();

        byte[] pdf = await generator.GenerateAsync(
            [ToReadModel(meal)],
            new DateTime(2026, 5, 3, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            locale: null,
            240,
            reportOrigin: null,
            CancellationToken.None);

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public void CalculateMealItemNutrition_WhenItemHasNoProductOrRecipe_ReturnsZeroNutrition() {
        var item = new MealConsumptionItemReadModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Amount: 1,
            ProductId: null,
            ProductName: null,
            ProductImageUrl: null,
            ProductBaseUnit: null,
            ProductBaseAmount: null,
            ProductCaloriesPerBase: null,
            ProductProteinsPerBase: null,
            ProductFatsPerBase: null,
            ProductCarbsPerBase: null,
            ProductFiberPerBase: null,
            ProductAlcoholPerBase: null,
            ProductType: null,
            RecipeId: null,
            RecipeName: null,
            RecipeImageUrl: null,
            RecipeServings: null,
            RecipeTotalCalories: null,
            RecipeTotalProteins: null,
            RecipeTotalFats: null,
            RecipeTotalCarbs: null,
            RecipeTotalFiber: null,
            RecipeTotalAlcohol: null,
            SourceAiItemId: null,
            MealItemOrigin.Manual);

        object nutrition = InvokePrivateStatic<object>("CalculateMealItemNutrition", item);

        Assert.Multiple(
            () => Assert.Equal(0d, GetPrivateProperty<double>(nutrition, "Calories")),
            () => Assert.Equal(0d, GetPrivateProperty<double>(nutrition, "Proteins")),
            () => Assert.Equal(0d, GetPrivateProperty<double>(nutrition, "Fats")),
            () => Assert.Equal(0d, GetPrivateProperty<double>(nutrition, "Carbs")),
            () => Assert.Equal(0d, GetPrivateProperty<double>(nutrition, "Fiber")));
    }

    [Fact]
    public void LayoutComposer_RendersImageCardAndEmptyMealsTableBranches() {
        var userId = UserId.New();
        Meal meal = CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 410, 12, 10, 40, 6);
        MealConsumptionReadModel mealReadModel = ToReadModel(meal);
        byte[] imageBytes = CreatePngBytes(width: 12, height: 12);
        object cardReport = CreateReportData([mealReadModel], mealImages: new Dictionary<Guid, byte[]> {
            [mealReadModel.Id] = imageBytes,
        });
        object emptyReport = CreateReportData([]);

        byte[] pdf = Document.Create(document => {
            document.Page(page => {
                page.Size(320, 420);
                page.Content().Column(column => {
                    column.Item().Element(container => InvokePrivateStatic<object?>("ComposeMealCard", container, cardReport, mealReadModel));
                    column.Item().Element(container => InvokePrivateStatic<object?>("ComposeMealsTable", container, emptyReport));
                });
            });
        }).GeneratePdf();

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public async Task GenerateAsync_WithIngredientImagesAndNoMealImage_DownloadsCollageSources() {
        var userId = UserId.New();
        Meal meal = CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 410, 12, 10, 40, 6);
        AddProductItem(meal, CreateProduct(userId, "Rice", "https://93.184.216.34/rice.png"), 150);
        AddProductItem(meal, CreateProduct(userId, "Carrot", "https://93.184.216.34/carrot.png"), 80);
        var imageHandler = new RecordingImageHandler(successfulImageResponse: true);
        var generator = new DiaryPdfGenerator(new HttpClient(imageHandler));

        byte[] pdf = await generator.GenerateAsync(
            [ToReadModel(meal)],
            new DateTime(2026, 5, 3, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            locale: null,
            240,
            reportOrigin: null,
            CancellationToken.None);

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.Equal(2, imageHandler.RequestCount);
        Assert.Contains("https://93.184.216.34/rice.png", imageHandler.RequestedUrls);
        Assert.Contains("https://93.184.216.34/carrot.png", imageHandler.RequestedUrls);
    }

    [Fact]
    public async Task GenerateAsync_WithAiSessionImagesAndNoMealImage_DownloadsCollageSources() {
        var userId = UserId.New();
        Meal meal = CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 410, 12, 10, 40, 6);
        AddAiSessionWithImage(meal, userId, "https://93.184.216.34/ai-1.png");
        AddAiSessionWithImage(meal, userId, "https://93.184.216.34/ai-2.png");
        var imageHandler = new RecordingImageHandler(successfulImageResponse: true);
        var generator = new DiaryPdfGenerator(new HttpClient(imageHandler));

        byte[] pdf = await generator.GenerateAsync(
            [ToReadModel(meal)],
            new DateTime(2026, 5, 3, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            locale: null,
            240,
            reportOrigin: null,
            CancellationToken.None);

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.Equal(2, imageHandler.RequestCount);
        Assert.Contains("https://93.184.216.34/ai-1.png", imageHandler.RequestedUrls);
        Assert.Contains("https://93.184.216.34/ai-2.png", imageHandler.RequestedUrls);
    }

    [Fact]
    public async Task GenerateAsync_WithRussianLocaleTimeZoneAndUnicodeOrigin_ReturnsPdfDocument() {
        var userId = UserId.New();
        Meal meal = CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 41, 1, 0, 10, 3);
        var generator = new DiaryPdfGenerator();

        byte[] pdf = await generator.GenerateAsync(
            [ToReadModel(meal)],
            new DateTime(2026, 5, 3, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            "ru",
            240,
            "https://Ð´Ð½ÐµÐ²Ð½Ð¸ÐºÐµÐ´Ñ‹.Ñ€Ñ„",
            CancellationToken.None);

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public async Task GenerateAsync_WithRecognizedItemsOnly_ReturnsPdfDocument() {
        var userId = UserId.New();
        Meal meal = CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 946, 59, 45, 76, 7);
        meal.AddAiSession(
            imageAssetId: null,
            AiRecognitionSource.Text,
            new DateTime(2026, 5, 4, 15, 3, 0, DateTimeKind.Utc),
            notes: null,
            [
                MealAiItemData.Create("carrot", "Ð¼Ð¾Ñ€ÐºÐ¾Ð²ÑŒ", 100, "g", 41, 1, 0, 10, 3, 0),
                MealAiItemData.Create("rice", "Ñ€Ð¸Ñ", 445, "g", 905, 58, 45, 66, 4, 0),
            ]);
        var generator = new DiaryPdfGenerator();

        byte[] pdf = await generator.GenerateAsync(
            [ToReadModel(meal)],
            new DateTime(2026, 5, 3, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            "ru",
            240,
            reportOrigin: null,
            CancellationToken.None);

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public async Task GenerateAsync_WithLongPeriod_DoesNotDownloadMealImages() {
        var userId = UserId.New();
        Meal meal = CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 41, 1, 0, 10, 3);
        meal.UpdateImage("https://example.test/meal.jpg");
        AddProductItem(meal, CreateProduct(userId, "Rice", "https://example.test/rice.png"), 150);
        var imageHandler = new RecordingImageHandler();
        var generator = new DiaryPdfGenerator(new HttpClient(imageHandler));

        byte[] pdf = await generator.GenerateAsync(
            [ToReadModel(meal)],
            new DateTime(2026, 4, 20, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            locale: null,
            240,
            reportOrigin: null,
            CancellationToken.None);

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.Equal(0, imageHandler.RequestCount);
    }

    [Fact]
    public async Task GenerateAsync_WithPrivateNetworkImageUrl_DoesNotRequestImage() {
        var userId = UserId.New();
        Meal meal = CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 41, 1, 0, 10, 3);
        meal.UpdateImage("http://127.0.0.1/admin.png");
        var imageHandler = new RecordingImageHandler(successfulImageResponse: true);
        var generator = new DiaryPdfGenerator(new HttpClient(imageHandler));

        byte[] pdf = await generator.GenerateAsync(
            [ToReadModel(meal)],
            new DateTime(2026, 5, 3, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            locale: null,
            240,
            reportOrigin: null,
            CancellationToken.None);

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.Equal(0, imageHandler.RequestCount);
    }

    [Fact]
    public async Task GenerateAsync_WithInvalidDataUrlImage_ReturnsPdfWithoutRequestingRemoteImage() {
        var userId = UserId.New();
        Meal meal = CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 41, 1, 0, 10, 3);
        meal.UpdateImage("data:image/png;base64,not-valid-base64");
        var imageHandler = new RecordingImageHandler(successfulImageResponse: true);
        var generator = new DiaryPdfGenerator(new HttpClient(imageHandler));

        byte[] pdf = await generator.GenerateAsync(
            [ToReadModel(meal)],
            new DateTime(2026, 5, 3, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            locale: null,
            240,
            reportOrigin: null,
            CancellationToken.None);

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.Equal(0, imageHandler.RequestCount);
    }

    [Fact]
    public async Task GenerateAsync_WhenRemoteImageReturnsNotFound_ReturnsPdfWithoutImage() {
        var userId = UserId.New();
        Meal meal = CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 41, 1, 0, 10, 3);
        meal.UpdateImage("https://93.184.216.34/missing.png");
        var imageHandler = new RecordingImageHandler();
        var generator = new DiaryPdfGenerator(new HttpClient(imageHandler));

        byte[] pdf = await generator.GenerateAsync(
            [ToReadModel(meal)],
            new DateTime(2026, 5, 3, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            locale: null,
            240,
            reportOrigin: null,
            CancellationToken.None);

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.Equal(1, imageHandler.RequestCount);
    }

    [Fact]
    public async Task GenerateAsync_WithDuplicateRemoteImageUrl_DownloadsImageOnce() {
        var userId = UserId.New();
        Meal firstMeal = CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 41, 1, 0, 10, 3);
        Meal secondMeal = CreateMeal(userId, new DateTime(2026, 5, 4, 18, 2, 0, DateTimeKind.Utc), 120, 5, 3, 20, 2);
        firstMeal.UpdateImage("https://93.184.216.34/shared.png");
        secondMeal.UpdateImage("https://93.184.216.34/shared.png");
        var imageHandler = new RecordingImageHandler(successfulImageResponse: true);
        var generator = new DiaryPdfGenerator(new HttpClient(imageHandler));

        byte[] pdf = await generator.GenerateAsync(
            [ToReadModel(firstMeal), ToReadModel(secondMeal)],
            new DateTime(2026, 5, 3, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            locale: null,
            240,
            reportOrigin: null,
            CancellationToken.None);

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.Equal(1, imageHandler.RequestCount);
    }

    [Fact]
    public void ResolveReportHost_HandlesBlankAbsoluteUnicodeAndInvalidValues() {
        Assert.Equal("fooddiary.club", InvokePrivateStatic<string>("ResolveReportHost", " "));
        Assert.Equal("fooddiary.club", InvokePrivateStatic<string>("ResolveReportHost", "https://fooddiary.club"));
        Assert.Equal("fooddiary.club:8443", InvokePrivateStatic<string>("ResolveReportHost", "https://fooddiary.club:8443/path"));
        Assert.Equal("пример.рф", InvokePrivateStatic<string>("ResolveReportHost", "xn--e1afmkfd.xn--p1ai"));
        Assert.Equal("fooddiary.club", InvokePrivateStatic<string>("ResolveReportHost", "bad host value"));
    }

    [Fact]
    public void ApplyAlpha_ConvertsValidHexAndLeavesInvalidHexUnchanged() {
        Assert.Equal("#80112233", InvokePrivateStatic<string>("ApplyAlpha", "#112233", 0.5));
        Assert.Equal("#00112233", InvokePrivateStatic<string>("ApplyAlpha", "112233", -1d));
        Assert.Equal("#FF112233", InvokePrivateStatic<string>("ApplyAlpha", "112233", 2d));
        Assert.Equal("#123", InvokePrivateStatic<string>("ApplyAlpha", "#123", 0.5));
    }

    [Fact]
    public void IsPublicAddress_RejectsPrivateLoopbackReservedAndLocalAddresses() {
        Assert.True(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("8.8.8.8")));
        Assert.True(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("2001:4860:4860::8888")));
        Assert.True(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("::ffff:8.8.8.8")));

        Assert.False(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("127.0.0.1")));
        Assert.False(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("0.0.0.1")));
        Assert.False(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("10.0.0.1")));
        Assert.False(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("100.64.0.1")));
        Assert.True(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("100.128.0.1")));
        Assert.True(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("169.253.255.255")));
        Assert.False(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("169.254.0.1")));
        Assert.True(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("172.15.255.255")));
        Assert.False(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("172.16.0.1")));
        Assert.True(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("172.32.0.1")));
        Assert.False(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("192.0.0.1")));
        Assert.True(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("192.1.0.1")));
        Assert.True(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("192.167.255.255")));
        Assert.False(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("192.168.0.1")));
        Assert.False(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("198.18.0.1")));
        Assert.True(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("198.20.0.1")));
        Assert.False(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("224.0.0.1")));
        Assert.False(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.IPv6Loopback));
        Assert.False(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("fe80::1")));
        Assert.False(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("fc00::1")));
        Assert.False(InvokePrivateStatic<bool>("IsPublicAddress", IPAddress.Parse("ff02::1")));
    }

    [Fact]
    public void TryReadDataUrl_ReturnsBytesOnlyForSupportedImageDataUrls() {
        Assert.True(TryReadDataUrl("data:image/png;base64,AQID", out byte[] bytes));
        Assert.Equal([1, 2, 3], bytes);

        Assert.False(TryReadDataUrl("data:text/plain;base64,AQID", out bytes));
        Assert.Empty(bytes);
        Assert.False(TryReadDataUrl("https://93.184.216.34/image.png", out bytes));
        Assert.Empty(bytes);
    }

    [Fact]
    public void ReportDayMode_NormalizesReversedAndLongPeriods() {
        Assert.False(InvokePrivateStatic<bool>(
            "ShouldUseCompactMealsMode",
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            new DateTime(2026, 5, 3, 20, 0, 0, DateTimeKind.Utc)));
        Assert.True(InvokePrivateStatic<bool>(
            "ShouldUseCompactMealsMode",
            new DateTime(2026, 4, 20, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc)));
    }

    [Fact]
    public void FormattingHelpers_FormatNumbersUnitsNamesAndHosts() {
        object report = CreateReportData([]);

        Assert.Equal("12", InvokePrivateStatic<string>("FormatNumber", 12.5d, 0));
        Assert.Equal("g", InvokePrivateStatic<string>("FormatUnit", "grams", report));
        Assert.Equal("ml", InvokePrivateStatic<string>("FormatUnit", " ml ", report));
        Assert.Equal("g", InvokePrivateStatic<string>("FormatUnit", null, report));
        Assert.Equal("", InvokePrivateStatic<string>("CapitalizeFirstLetter", "", CultureInfo.InvariantCulture));
        Assert.Equal("Rice", InvokePrivateStatic<string>("CapitalizeFirstLetter", " rice", CultureInfo.InvariantCulture));
        Assert.Equal("fooddiary.club", InvokePrivateStatic<string>("ResolveReportHost", "http://[::1"));
        Assert.Equal("fooddiary.club", InvokePrivateStatic<string>("ToUnicodeHost", "xn--"));
    }

    [Fact]
    public void MealItemFormatting_ReturnsFallbacksSuffixesAndNutrition() {
        var userId = UserId.New();
        Meal emptyMeal = CreateMeal(userId, DateTime.UtcNow, 0, 0, 0, 0, 0);
        MealConsumptionReadModel emptyMealReadModel = ToReadModel(emptyMeal);
        object report = CreateReportData([emptyMealReadModel]);

        Assert.Equal("not specified", InvokePrivateStatic<string>("FormatMealItemsList", emptyMealReadModel, report));
        Assert.Equal("Items: not specified", InvokePrivateStatic<string>("FormatMealItems", emptyMealReadModel, report));

        Meal meal = CreateMeal(userId, DateTime.UtcNow, 0, 0, 0, 0, 0);
        for (int index = 0; index < 7; index++) {
            AddProductItem(meal, CreateProduct(userId, $"item-{index.ToString(CultureInfo.InvariantCulture)}", imageUrl: ""), 100 + index);
        }

        string labels = InvokePrivateStatic<string>("FormatMealItemsList", ToReadModel(meal), report);

        Assert.Contains("+1 more", labels, StringComparison.Ordinal);
    }

    [Fact]
    public void MealItemFormatting_CoversProductRecipeAiAndFallbackNutritionBranches() {
        var userId = UserId.New();
        object enReport = CreateReportData([], cultureName: "en");
        object ruReport = CreateReportData([], cultureName: "ru");
        var meal = Meal.Create(userId, DateTime.UtcNow, MealType.Lunch);
        Product product = CreateProduct(userId, "rice", imageUrl: "");
        MealItem productItem = meal.AddProduct(product.Id, 50);
        typeof(MealItem).GetProperty(nameof(MealItem.Product))!.SetValue(productItem, product);
        var recipe = Recipe.Create(userId, "soup", servings: 2);
        recipe.ApplyComputedNutrition(200, 10, 4, 30, 6, 0);
        MealItem recipeItem = meal.AddRecipe(recipe.Id, 1);
        typeof(MealItem).GetProperty(nameof(MealItem.Recipe))!.SetValue(recipeItem, recipe);
        MealItem fallbackItem = meal.AddProduct(ProductId.New(), 25);
        MealAiSession session = meal.AddAiSession(
            imageAssetId: null,
            AiRecognitionSource.Text,
            DateTime.UtcNow,
            notes: null,
            [
                MealAiItemData.Create(" apple ", " local apple ", 80, "grams", 42, 1, 0, 10, 2, 0),
                MealAiItemData.Create("fallback", "", 10, "g", 1, 0, 0, 0, 0, 0),
            ]);
        typeof(MealAiItem)
            .GetProperty(nameof(MealAiItem.NameEn))!
            .SetValue(session.Items.Last(), " ");

        object productComposition = InvokePrivateStatic<object>("FormatMealItem", ToReadModel(productItem), enReport);
        object recipeComposition = InvokePrivateStatic<object>("FormatMealItem", ToReadModel(recipeItem), enReport);
        object fallbackComposition = InvokePrivateStatic<object>("FormatMealItem", ToReadModel(fallbackItem), enReport);
        object localAiComposition = InvokePrivateStatic<object>("FormatMealAiItem", ToReadModel(session.Items.First()), ruReport);
        object fallbackAiComposition = InvokePrivateStatic<object>("FormatMealAiItem", ToReadModel(session.Items.Last()), enReport);

        Assert.Equal("Rice", GetPrivateProperty<string>(productComposition, "Name"));
        Assert.Equal(60, GetPrivateProperty<double>(productComposition, "Calories"));
        Assert.Equal("Soup", GetPrivateProperty<string>(recipeComposition, "Name"));
        Assert.Equal(100, GetPrivateProperty<double>(recipeComposition, "Calories"));
        Assert.Equal("Product", GetPrivateProperty<string>(fallbackComposition, "Name"));
        Assert.Equal(0, GetPrivateProperty<double>(fallbackComposition, "Calories"));
        Assert.Equal("Local apple", GetPrivateProperty<string>(localAiComposition, "Name"));
        Assert.Equal("Product", GetPrivateProperty<string>(fallbackAiComposition, "Name"));
    }

    [Fact]
    public void DiaryReportData_CreateNormalizesDatesOffsetsCultureAndMealTypes() {
        var userId = UserId.New();
        Meal dinner = CreateMeal(userId, new DateTime(2026, 5, 4, 20, 0, 0, DateTimeKind.Utc), 100, 10, 5, 20, 3);
        object report = CreateReportData(
            [ToReadModel(dinner)],
            dateFrom: new DateTime(2026, 5, 5, 18, 0, 0, DateTimeKind.Unspecified),
            dateTo: new DateTime(2026, 5, 3, 18, 0, 0, DateTimeKind.Utc),
            cultureName: "bad-culture",
            timeZoneOffsetMinutes: 900);

        Assert.Equal(2, GetPrivateProperty<int>(report, "DayCount"));
        Assert.Equal("UTC+06:00", GetPrivateProperty<string>(report, "TimeZoneOffsetLabel"));
        Assert.Equal("Other", InvokePrivateInstance<string>(report, "FormatMealType", (MealType?)null));
        Assert.Equal("Breakfast", InvokePrivateInstance<string>(report, "FormatMealType", (MealType?)MealType.Breakfast));
        Assert.Equal("Lunch", InvokePrivateInstance<string>(report, "FormatMealType", (MealType?)MealType.Lunch));
        Assert.Equal("Dinner", InvokePrivateInstance<string>(report, "FormatMealType", (MealType?)MealType.Dinner));
        Assert.Equal("Snack", InvokePrivateInstance<string>(report, "FormatMealType", (MealType?)MealType.Snack));
        Assert.StartsWith("2026-", InvokePrivateInstance<string>(report, "FormatMealDate", dinner.Date), StringComparison.Ordinal);
        Assert.StartsWith("2026-", InvokePrivateInstance<string>(
            report,
            "FormatMealDate",
            DateTime.SpecifyKind(dinner.Date, DateTimeKind.Local)), StringComparison.Ordinal);
    }

    [Fact]
    public void ChartSvgRenderer_CoversEmptySingleMultiPointAndEscapedLabels() {
        Type renderer = typeof(DiaryPdfGenerator).GetNestedType("DiaryChartSvgRenderer", BindingFlags.NonPublic)!;
        string lineChart = InvokeStatic<string>(
            renderer,
            "RenderLineChart",
            new[] { "<day>", "next" },
            new double[] { 0, 1234 },
            "#111111",
            "#222222");
        string sparkline = InvokeStatic<string>(renderer, "RenderSparkline", Array.Empty<double>(), "#111111", "#222222");
        string wideSparkline = InvokeStatic<string>(renderer, "RenderWideSparkline", new double[] { 5 }, "#111111", "#222222");
        Type pointType = renderer.GetNestedType("Point", BindingFlags.NonPublic)!;
        var emptyPoints = Array.CreateInstance(pointType, 0);

        Assert.Contains("&lt;day&gt;", lineChart, StringComparison.Ordinal);
        Assert.Contains("1,000", lineChart, StringComparison.Ordinal);
        Assert.Contains("<svg", sparkline, StringComparison.Ordinal);
        Assert.Contains("<svg", wideSparkline, StringComparison.Ordinal);
        Assert.Equal("", InvokeStatic<string>(renderer, "BuildSmoothPath", emptyPoints));
        Assert.Equal("", InvokeStatic<string>(renderer, "BuildAreaPath", emptyPoints, 100d));
    }

    [Fact]
    public void ImageHelpers_PrepareCropEncodeCollageAndSlots() {
        byte[] imageBytes = CreatePngBytes(width: 12, height: 20);

        byte[]? prepared = InvokePrivateStatic<byte[]?>("PrepareMealImage", imageBytes);
        byte[]? emptyCollage = InvokePrivateStatic<byte[]?>("CreateMealImageCollage", (object)Array.Empty<byte[]>());
        byte[]? singleCollage = InvokePrivateStatic<byte[]?>("CreateMealImageCollage", (object)new[] { imageBytes });
        byte[]? multiCollage = InvokePrivateStatic<byte[]?>("CreateMealImageCollage", (object)new[] { imageBytes, imageBytes, imageBytes, imageBytes });
        Array twoSlots = InvokePrivateStatic<Array>("GetCollageSlots", 2);
        Array threeSlots = InvokePrivateStatic<Array>("GetCollageSlots", 3);
        Array fourSlots = InvokePrivateStatic<Array>("GetCollageSlots", 4);
        using var bitmap = new SKBitmap(10, 20);
        using SKBitmap cropped = InvokePrivateStatic<SKBitmap>("CreateCroppedBitmap", bitmap, 24, 24);
        byte[] encoded = InvokePrivateStatic<byte[]>("EncodeMealImage", cropped);

        Assert.NotNull(prepared);
        Assert.Null(emptyCollage);
        Assert.Same(imageBytes, singleCollage);
        Assert.NotNull(multiCollage);
        Assert.Equal(2, twoSlots.Length);
        Assert.Equal(3, threeSlots.Length);
        Assert.Equal(4, fourSlots.Length);
        Assert.Equal(24, cropped.Width);
        Assert.Equal(24, cropped.Height);
        Assert.NotEmpty(encoded);
    }

    [Fact]
    public void ImageHelpers_ReturnNullForInvalidBytesAndComposeImageRenders() {
        byte[] imageBytes = CreatePngBytes(width: 10, height: 10);
        byte[][] invalidImages = [[1, 2, 3], [4, 5, 6]];
        byte[][] mixedImages = [[], CreatePngBytes(width: 10, height: 10)];

        byte[]? invalidPrepared = InvokePrivateStatic<byte[]?>("PrepareMealImage", (object)(byte[])[1, 2, 3]);
        byte[]? invalidCollage = InvokePrivateStatic<byte[]?>("CreateMealImageCollage", (object)invalidImages);
        byte[]? mixedCollage = InvokePrivateStatic<byte[]?>("CreateMealImageCollage", (object)mixedImages);
        byte[] pdf = Document.Create(document => {
            document.Page(page => {
                page.Size(120, 120);
                page.Content().Element(container => InvokePrivateStatic<object?>("ComposeMealImage", container, imageBytes));
            });
        }).GeneratePdf();

        Assert.Null(invalidPrepared);
        Assert.Null(invalidCollage);
        Assert.True(mixedCollage is null || mixedCollage.Length > 0);
        Assert.True(pdf.Length > 1024);
    }

    [Fact]
    public void TryReadDataUrl_WhenBase64IsTooLarge_ReturnsFalse() {
        string oversized = $"data:image/png;base64,{new string('A', 3 * 1024 * 1024)}";

        Assert.False(TryReadDataUrl(oversized, out byte[] bytes));
        Assert.Empty(bytes);
    }

    [Fact]
    public async Task IsAllowedRemoteImageUriAsync_CoversSchemeLocalhostLiteralAndUnresolvableAddresses() {
        Assert.False(await InvokePrivateStatic<Task<bool>>("IsAllowedRemoteImageUriAsync", new Uri("ftp://example.com/image.png"), CancellationToken.None));
        Assert.False(await InvokePrivateStatic<Task<bool>>("IsAllowedRemoteImageUriAsync", new Uri("https://localhost/image.png"), CancellationToken.None));
        Assert.False(await InvokePrivateStatic<Task<bool>>("IsAllowedRemoteImageUriAsync", new Uri("https://assets.localhost/image.png"), CancellationToken.None));
        Assert.False(await InvokePrivateStatic<Task<bool>>("IsAllowedRemoteImageUriAsync", new Uri("http://127.0.0.1/image.png"), CancellationToken.None));
        Assert.True(await InvokePrivateStatic<Task<bool>>("IsAllowedRemoteImageUriAsync", new Uri("https://93.184.216.34/image.png"), CancellationToken.None));
        Assert.False(await InvokePrivateStatic<Task<bool>>("IsAllowedRemoteImageUriAsync", new Uri("https://unresolvable.invalid/image.png"), CancellationToken.None));
    }

    [Fact]
    public async Task IsAllowedRemoteImageUriAsync_WhenDnsResolverThrowsSocketException_ReturnsFalse() {
        Func<string, CancellationToken, Task<IPAddress[]>> originalResolver = DiaryPdfGenerator.RemoteImageHostResolver;
        DiaryPdfGenerator.RemoteImageHostResolver = (_, _) => Task.FromException<IPAddress[]>(new System.Net.Sockets.SocketException());
        try {
            bool allowed = await InvokePrivateStatic<Task<bool>>(
                "IsAllowedRemoteImageUriAsync",
                new Uri("https://images.example.test/image.png"),
                CancellationToken.None);

            Assert.False(allowed);
        } finally {
            DiaryPdfGenerator.RemoteImageHostResolver = originalResolver;
        }
    }

    [Fact]
    public async Task IsAllowedRemoteImageUriAsync_WhenDnsResolverReturnsNoAddresses_ReturnsFalse() {
        Func<string, CancellationToken, Task<IPAddress[]>> originalResolver = DiaryPdfGenerator.RemoteImageHostResolver;
        DiaryPdfGenerator.RemoteImageHostResolver = (_, _) => Task.FromResult(Array.Empty<IPAddress>());
        try {
            bool allowed = await InvokePrivateStatic<Task<bool>>(
                "IsAllowedRemoteImageUriAsync",
                new Uri("https://images.example.test/image.png"),
                CancellationToken.None);

            Assert.False(allowed);
        } finally {
            DiaryPdfGenerator.RemoteImageHostResolver = originalResolver;
        }
    }

    [Fact]
    public async Task IsAllowedRemoteImageUriAsync_WhenDnsResolverReturnsPrivateAddress_ReturnsFalse() {
        Func<string, CancellationToken, Task<IPAddress[]>> originalResolver = DiaryPdfGenerator.RemoteImageHostResolver;
        DiaryPdfGenerator.RemoteImageHostResolver = (_, _) => Task.FromResult<IPAddress[]>([IPAddress.Parse("10.0.0.1")]);
        try {
            bool allowed = await InvokePrivateStatic<Task<bool>>(
                "IsAllowedRemoteImageUriAsync",
                new Uri("https://images.example.test/image.png"),
                CancellationToken.None);

            Assert.False(allowed);
        } finally {
            DiaryPdfGenerator.RemoteImageHostResolver = originalResolver;
        }
    }

    [Fact]
    public void EnsureUtcForReport_CoversLocalAndUnspecifiedKinds() {
        DateTime unspecified = new(2026, 5, 4, 15, 0, 0, DateTimeKind.Unspecified);
        DateTime local = new(2026, 5, 4, 15, 0, 0, DateTimeKind.Local);

        DateTime normalizedUnspecified = InvokePrivateStatic<DateTime>("EnsureUtcForReport", unspecified);
        DateTime normalizedLocal = InvokePrivateStatic<DateTime>("EnsureUtcForReport", local);

        Assert.Equal(DateTimeKind.Utc, normalizedUnspecified.Kind);
        Assert.Equal(unspecified, DateTime.SpecifyKind(normalizedUnspecified, DateTimeKind.Unspecified));
        Assert.Equal(DateTimeKind.Utc, normalizedLocal.Kind);
    }

    [Fact]
    public async Task LoadMealImageAsync_CoversEmptyOversizedAndValidDataUrlResponses() {
        var emptyHandler = new RecordingImageHandler(successfulImageResponse: true, content: []);
        var emptyGenerator = new DiaryPdfGenerator(new HttpClient(emptyHandler));
        var oversizedHandler = new RecordingImageHandler(successfulImageResponse: true, contentLength: 3 * 1024 * 1024);
        var oversizedGenerator = new DiaryPdfGenerator(new HttpClient(oversizedHandler));
        var validGenerator = new DiaryPdfGenerator();
        string dataUrl = $"data:image/png;base64,{Convert.ToBase64String(CreatePngBytes(width: 1, height: 1))}";

        byte[]? empty = await InvokePrivateInstance<Task<byte[]?>>(emptyGenerator, "LoadMealImageAsync", "https://93.184.216.34/empty.png", CancellationToken.None);
        byte[]? oversized = await InvokePrivateInstance<Task<byte[]?>>(oversizedGenerator, "LoadMealImageAsync", "https://93.184.216.34/large.png", CancellationToken.None);
        byte[]? valid = await InvokePrivateInstance<Task<byte[]?>>(validGenerator, "LoadMealImageAsync", dataUrl, CancellationToken.None);

        Assert.Null(empty);
        Assert.Null(oversized);
        Assert.NotNull(valid);
    }

    [Fact]
    public async Task LoadMealImageAsync_CoversInvalidUriHttpExceptionAndStreamingLimit() {
        var throwingGenerator = new DiaryPdfGenerator(new HttpClient(new ThrowingImageHandler()));
        var streamingGenerator = new DiaryPdfGenerator(new HttpClient(new RecordingImageHandler(
            successfulImageResponse: true,
            content: new byte[3 * 1024 * 1024])));
        var chunkedGenerator = new DiaryPdfGenerator(new HttpClient(new ChunkedImageHandler(
            chunkSize: 81920,
            totalBytes: 3 * 1024 * 1024)));

        byte[]? invalidUri = await InvokePrivateInstance<Task<byte[]?>>(
            new DiaryPdfGenerator(),
            "LoadMealImageAsync",
            "not a uri",
            CancellationToken.None);
        byte[]? exception = await InvokePrivateInstance<Task<byte[]?>>(
            throwingGenerator,
            "LoadMealImageAsync",
            "https://93.184.216.34/fail.png",
            CancellationToken.None);
        byte[]? tooLarge = await InvokePrivateInstance<Task<byte[]?>>(
            streamingGenerator,
            "LoadMealImageAsync",
            "https://93.184.216.34/stream.png",
            CancellationToken.None);
        byte[]? tooLargeByLoop = await InvokePrivateInstance<Task<byte[]?>>(
            chunkedGenerator,
            "LoadMealImageAsync",
            "https://93.184.216.34/chunked.png",
            CancellationToken.None);

        Assert.Null(invalidUri);
        Assert.Null(exception);
        Assert.Null(tooLarge);
        Assert.Null(tooLargeByLoop);
    }

    [Fact]
    public async Task LoadMealImagesAsync_WhenMealImageDecodes_ReturnsDictionaryEntry() {
        string dataUrl = $"data:image/png;base64,{Convert.ToBase64String(CreatePngBytes(width: 16, height: 16))}";
        var userId = UserId.New();
        Meal meal = CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 41, 1, 0, 10, 3);
        meal.UpdateImage(dataUrl);
        var generator = new DiaryPdfGenerator();

        MealConsumptionReadModel mealReadModel = ToReadModel(meal);
        IReadOnlyDictionary<Guid, byte[]> images = await InvokePrivateInstance<Task<IReadOnlyDictionary<Guid, byte[]>>>(
            generator,
            "LoadMealImagesAsync",
            (IReadOnlyList<MealConsumptionReadModel>)[mealReadModel],
            CancellationToken.None);

        Assert.True(images.ContainsKey(mealReadModel.Id));
        Assert.NotEmpty(images[mealReadModel.Id]);
    }

    [Fact]
    public void CreateMealImageCollage_WhenTileFailsToDecode_SkipsItAndRendersRemaining() {
        // SKBitmap.Decode returns null (without throwing) for this 1x1 transparent PNG.
        byte[] undecodableTile = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");
        byte[] validTile = CreatePngBytes(width: 16, height: 16);

        byte[]? collage = InvokePrivateStatic<byte[]?>(
            "CreateMealImageCollage",
            (object)new[] { undecodableTile, validTile });

        Assert.NotNull(collage);
    }

    private static Meal CreateMeal(
        UserId userId,
        DateTime date,
        double calories,
        double proteins,
        double fats,
        double carbs,
        double fiber) {
        var meal = Meal.Create(userId, date, MealType.Lunch);
        meal.ApplyNutrition(new MealNutritionUpdate(calories, proteins, fats, carbs, fiber, 0, IsAutoCalculated: true));
        return meal;
    }

    private static MealConsumptionReadModel ToReadModel(Meal meal) =>
        new(
            meal.Id.Value,
            meal.Date,
            meal.MealType,
            meal.Comment,
            meal.ImageUrl,
            meal.ImageAssetId?.Value,
            meal.TotalCalories,
            meal.TotalProteins,
            meal.TotalFats,
            meal.TotalCarbs,
            meal.TotalFiber,
            meal.TotalAlcohol,
            meal.IsNutritionAutoCalculated,
            meal.ManualCalories,
            meal.ManualProteins,
            meal.ManualFats,
            meal.ManualCarbs,
            meal.ManualFiber,
            meal.ManualAlcohol,
            meal.PreMealSatietyLevel,
            meal.PostMealSatietyLevel,
            [.. meal.Items.OrderBy(static item => item.Id.Value).Select(ToReadModel)],
            [.. meal.AiSessions.OrderBy(static session => session.RecognizedAtUtc).Select(ToReadModel)]);

    private static MealConsumptionItemReadModel ToReadModel(MealItem item) =>
        new(
            item.Id.Value,
            item.MealId.Value,
            item.Amount,
            item.ProductId?.Value,
            item.Product?.Name,
            item.Product?.ImageUrl,
            item.Product?.BaseUnit.ToString(),
            item.Product?.BaseAmount,
            item.Product?.CaloriesPerBase,
            item.Product?.ProteinsPerBase,
            item.Product?.FatsPerBase,
            item.Product?.CarbsPerBase,
            item.Product?.FiberPerBase,
            item.Product?.AlcoholPerBase,
            item.Product?.ProductType,
            item.RecipeId?.Value,
            item.Recipe?.Name,
            item.Recipe?.ImageUrl,
            item.HasNutritionSnapshot ? 1 : item.Recipe?.Servings,
            item.Recipe?.TotalCalories,
            item.Recipe?.TotalProteins,
            item.Recipe?.TotalFats,
            item.Recipe?.TotalCarbs,
            item.Recipe?.TotalFiber,
            item.Recipe?.TotalAlcohol,
            item.SourceAiItemId?.Value,
            item.Origin);

    private static MealConsumptionAiSessionReadModel ToReadModel(MealAiSession session) =>
        new(
            session.Id.Value,
            session.MealId.Value,
            session.ImageAssetId?.Value,
            session.ImageAsset?.Url,
            session.Source,
            session.Status,
            session.RecognizedAtUtc,
            session.Notes,
            [.. session.Items.OrderBy(static item => item.Id.Value).Select(ToReadModel)]);

    private static MealConsumptionAiItemReadModel ToReadModel(MealAiItem item) =>
        new(
            item.Id.Value,
            item.MealAiSessionId.Value,
            item.NameEn,
            item.NameLocal,
            item.Amount,
            item.Unit,
            item.Calories,
            item.Proteins,
            item.Fats,
            item.Carbs,
            item.Fiber,
            item.Alcohol,
            item.Confidence,
            item.Resolution);

    private static Product CreateProduct(UserId userId, string name, string imageUrl) =>
        Product.Create(
            userId,
            name,
            MeasurementUnit.G,
            100,
            100,
            caloriesPerBase: 120,
            proteinsPerBase: 3,
            fatsPerBase: 1,
            carbsPerBase: 20,
            fiberPerBase: 2,
            alcoholPerBase: 0,
            imageUrl: imageUrl);

    private static void AddProductItem(Meal meal, Product product, double amount) {
        MealItem item = meal.AddProduct(product.Id, amount);
        typeof(MealItem)
            .GetProperty(nameof(MealItem.Product))!
            .SetValue(item, product);
    }

    private static void AddAiSessionWithImage(Meal meal, UserId userId, string imageUrl) {
        var asset = ImageAsset.Create(userId, $"meals/{Guid.NewGuid():N}.png", imageUrl);
        MealAiSession session = meal.AddAiSession(
            asset.Id,
            AiRecognitionSource.Photo,
            DateTime.UtcNow,
            notes: null,
            [
                MealAiItemData.Create("rice", nameLocal: null, 100, "g", 120, 3, 1, 20, 2, 0),
            ]);
        typeof(MealAiSession)
            .GetProperty(nameof(MealAiSession.ImageAsset))!
            .SetValue(session, asset);
    }

    private static T InvokePrivateStatic<T>(string methodName, params object?[] arguments) {
        MethodInfo method = typeof(DiaryPdfGenerator)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(candidate =>
                string.Equals(candidate.Name, methodName, StringComparison.Ordinal) &&
                candidate.GetParameters().Length == arguments.Length);
        return (T)method.Invoke(null, arguments)!;
    }

    private static T InvokePrivateInstance<T>(object instance, string methodName, params object?[] arguments) {
        MethodInfo method = instance.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(candidate =>
                string.Equals(candidate.Name, methodName, StringComparison.Ordinal) &&
                candidate.GetParameters().Length == arguments.Length);
        return (T)method.Invoke(instance, arguments)!;
    }

    private static T InvokeStatic<T>(Type type, string methodName, params object?[] arguments) {
        MethodInfo method = type
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(candidate =>
                string.Equals(candidate.Name, methodName, StringComparison.Ordinal) &&
                candidate.GetParameters().Length == arguments.Length);
        return (T)method.Invoke(null, arguments)!;
    }

    private static T GetPrivateProperty<T>(object instance, string propertyName) {
        PropertyInfo property = instance.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        return (T)property.GetValue(instance)!;
    }

    private static object CreateReportData(
        IReadOnlyList<MealConsumptionReadModel> meals,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string cultureName = "en",
        int? timeZoneOffsetMinutes = 240,
        IReadOnlyDictionary<Guid, byte[]>? mealImages = null,
        bool compactMealsMode = false) {
        Type reportType = typeof(DiaryPdfGenerator).GetNestedType("DiaryReportData", BindingFlags.NonPublic)!;
        MethodInfo create = reportType.GetMethod("Create", BindingFlags.Static | BindingFlags.Public)!;
        return create.Invoke(null, [
            meals,
            dateFrom ?? new DateTime(2026, 5, 3, 20, 0, 0, DateTimeKind.Utc),
            dateTo ?? new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc),
            mealImages ?? new Dictionary<Guid, byte[]>(),
            compactMealsMode,
            CreateReportTexts(cultureName),
            timeZoneOffsetMinutes,
            "fooddiary.club",
            FixedGeneratedAtUtc,
        ])!;
    }

    private static readonly DateTime FixedGeneratedAtUtc = new(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);

    private static DiaryPdfReportTexts CreateReportTexts(string cultureName) =>
        new(
            CultureName: cultureName,
            ReportTitle: "Food Diary Report",
            PeriodLabel: "Period",
            MealsCountLabel: "{0} meals",
            PeriodSummaryTitle: "Period summary",
            TotalCaloriesTitle: "Total calories",
            KcalUnit: "kcal",
            AveragePerDayTitle: "Average per day",
            TotalForPeriodTitle: "Total for period",
            ProteinsTitle: "Proteins",
            FatsTitle: "Fats",
            CarbsTitle: "Carbs",
            FiberTitle: "Fiber",
            GramsUnit: "g",
            GramsProteinsLabel: "g proteins",
            GramsFatsLabel: "g fats",
            GramsCarbsLabel: "g carbs",
            GramsFiberLabel: "g fiber",
            CaloriesByDayTitle: "Calories by day",
            NutrientsByDayTitle: "Nutrients by day",
            MealsTitle: "Meals",
            NoMealsMessage: "No meals recorded in this period.",
            DateColumn: "Date",
            TypeColumn: "Type",
            ItemsColumn: "Items",
            AmountColumn: "Amount",
            KcalColumn: "Kcal",
            ProteinsColumnShort: "Proteins, g",
            FatsColumnShort: "Fats, g",
            CarbsColumnShort: "Carbs, g",
            FiberColumnShort: "Fiber, g",
            SatietyColumn: "Satiety",
            CommentColumn: "Comment",
            BeforeLabel: "Hunger before",
            AfterLabel: "Satiety after",
            OtherMealType: "Other",
            BreakfastMealType: "Breakfast",
            LunchMealType: "Lunch",
            DinnerMealType: "Dinner",
            SnackMealType: "Snack",
            ItemsPrefix: "Items",
            ItemsNotSpecified: "not specified",
            MoreItemsSuffix: "more",
            RecipeFallback: "Recipe",
            ProductFallback: "Product",
            ServingUnit: "serv.",
            GeneratedByPrefix: "Generated by Food Diary - ");

    private static byte[] CreatePngBytes(int width, int height) {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Red);
        using SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static bool TryReadDataUrl(string value, out byte[] bytes) {
        MethodInfo method = typeof(DiaryPdfGenerator)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(candidate => string.Equals(candidate.Name, "TryReadDataUrl", StringComparison.Ordinal));
        object?[] arguments = [value, null];
        bool result = (bool)method.Invoke(null, arguments)!;
        bytes = (byte[])arguments[1]!;
        return result;
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingImageHandler : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("Image request failed.");
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingImageHandler(
        bool successfulImageResponse = false,
        byte[]? content = null,
        long? contentLength = null) : HttpMessageHandler {
        private const string TransparentPngBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=";

        private readonly bool _successfulImageResponse = successfulImageResponse;

        public int RequestCount { get; private set; }
        public IReadOnlyList<string> RequestedUrls => _requestedUrls;

        private readonly List<string> _requestedUrls = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            RequestCount++;
            _requestedUrls.Add(request.RequestUri?.ToString() ?? "");

            if (!_successfulImageResponse) {
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
            }

            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
                Content = new ByteArrayContent(content ?? Convert.FromBase64String(TransparentPngBase64)),
            };
            if (contentLength.HasValue) {
                response.Content.Headers.ContentLength = contentLength.Value;
            }

            return Task.FromResult(response);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ChunkedImageHandler(int chunkSize, int totalBytes) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK) {
                Content = new StreamContent(new ChunkedReadStream(chunkSize, totalBytes)),
            };
            return Task.FromResult(response);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ChunkedReadStream(int chunkSize, int totalBytes) : Stream {
        private readonly int _totalBytes = totalBytes;
        private int _remaining = totalBytes;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _totalBytes;
        public override long Position { get; set; }

        public override int Read(byte[] buffer, int offset, int count) {
            if (_remaining <= 0) {
                return 0;
            }

            int read = Math.Min(Math.Min(chunkSize, count), _remaining);
            Array.Fill(buffer, (byte)1, offset, read);
            _remaining -= read;
            Position += read;
            return read;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
            if (_remaining <= 0) {
                return ValueTask.FromResult(0);
            }

            int read = Math.Min(Math.Min(chunkSize, buffer.Length), _remaining);
            buffer[..read].Span.Fill(1);
            _remaining -= read;
            Position += read;
            return ValueTask.FromResult(read);
        }

        public override void Flush() {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
